using iText.Kernel.Pdf;
using iText.Kernel.Utils;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using UvA.Workflow.Entities.Domain;
using UvA.Workflow.Persistence;
using UvA.Workflow.Tools;
using UvA.Workflow.Users;
using UvA.Workflow.WorkflowInstances;

namespace UvA.Workflow.Security.Pdf;

public enum Language { En, Nl }

public class PdfService(ModelService modelService, IWorkflowInstanceRepository instanceRepository,
     RightsService rightsService, IArtifactService artifactService)
{
    static PdfService() => QuestPDF.Settings.License = LicenseType.Community;
    
    public async Task<byte[]> GenerateInstancesPdf(string[] instanceIds, Language language,
        CancellationToken ct)
    {
        var insts = (await instanceRepository.GetByIds(instanceIds, ct)).ToArray();
        
        var contexts = insts.ToDictionary(i => i.Id, modelService.CreateContext);
            
        var docs = new List<IDocument>();
        foreach (var inst in insts)
            docs.AddRange(await GetInstanceDocuments(inst, contexts[inst.Id], language));
        
        var doc = QuestPDF.Fluent.Document.Merge(docs);
        
        var docBytes = doc.UseContinuousPageNumbers().GeneratePdf();
        return docBytes;
    }

    private string? GetLogoUrl(WorkflowInstance inst)
    {
        var insts = inst.Properties["Institution"].AsBsonArray.Select(a => a.AsString).ToArray();
        if (insts.Length == 0) insts = ["", ""];
        return insts[0] switch
        {
            _ when insts.Length == 2 => "https://content.datanose.nl/hvauvalogo.jpg",
            "UvA" => "https://content.datanose.nl/uvalogo_regular_p_nl.jpg",
            "HvA" => "https://content.datanose.nl/hvalogo.png",
            _ => null
        };
    }

    private async Task<IEnumerable<IDocument>> GetInstanceDocuments(WorkflowInstance inst, ObjectContext context,
        Language language, string[]? forms = null)
    {
        var allowed = await rightsService.GetAllowedActions(inst, RoleAction.View);
        
        forms ??= allowed.SelectMany(f => f.AllForms).ToArray();
        
        byte[]? logo = null;
        var url = GetLogoUrl(inst);
        if (url != null)
            logo = await new HttpClient().GetByteArrayAsync(url);
        
        return [
            new InstanceDocument(modelService, inst, language, logo, context),
            ..forms
                .Where(inst.HasEvent)
                .Select(s => new FormDocument(inst, modelService.GetForm(inst, s), language))
        ];
    }
    
    public async Task<byte[]> GenerateInstancePdf(string instanceId, string[] forms, Language language,
        CancellationToken ct)
    {
        var inst = await instanceRepository.GetById(instanceId, ct);
        if (inst == null) throw new ArgumentException("Invalid instance ID", nameof(instanceId));
        
        var context = modelService.CreateContext(inst);
        
        var doc = QuestPDF.Fluent.Document.Merge(await GetInstanceDocuments(inst, context, language, forms));
        
        return doc.UseContinuousPageNumbers().GeneratePdf();
    }
    
    public async Task<byte[]> GenerateSubmissionPdf(string instanceId, string formName, Language language,
        CancellationToken ct)
    {
        var inst = await instanceRepository.GetById(instanceId, ct);
        if (inst == null) throw new ArgumentException("Invalid instance ID", nameof(instanceId));
        
        var form = modelService.GetForm(inst, formName);
        
        var doc = new FormDocument(inst, form, language);
        var formBytes = doc.GeneratePdf();

        var fileProperties = form.PropertyDefinitions.Where(p => p.DataType == DataType.File);
        
        var context = modelService.CreateContext(inst);

        var files = fileProperties.SelectMany(p => context.Get(p.Name) switch
        {
            ArtifactInfo ai => [ai],
            ArtifactInfo[] ais => ais,
            _ => []
        }).Where(p => p.Name.ToLower().EndsWith(".pdf")).ToArray();
        if (files.Length == 0)
            return formBytes;

        var artifacts = await files.SelectAsync((f, t) => artifactService.GetArtifact(f.Id, t), ct);

        return MergePdfs([formBytes, ..artifacts.Where(f => f != null).Select(f => f!.Content)]);
    }

    private static byte[] MergePdfs(IEnumerable<byte[]> pdfs)
    {
        using var targetStream = new MemoryStream();
        using (var target = new PdfDocument(new PdfWriter(targetStream)))
        {
            var merger = new PdfMerger(target);
            foreach (var pdf in pdfs)
            {
                using var sourceStream = new MemoryStream(pdf);
                try
                {
                    using var source = new PdfDocument(new PdfReader(sourceStream));
                    merger.Merge(source, 1, source.GetNumberOfPages());
                }
                catch
                {
                    // ignore invalid pdf
                }
            }
        }

        return targetStream.ToArray();
    }
}