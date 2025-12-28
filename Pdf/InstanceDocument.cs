using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using UvA.Workflow.Entities.Domain;
using UvA.Workflow.WorkflowInstances;

namespace UvA.Workflow.Security.Pdf;


public abstract class Document(Language language) : IDocument
{
    protected string Localize(string en, string nl) => language == Language.En ? en : nl;
    protected string Localize(BilingualString str) => language == Language.En ? str.En : str.Nl;
    
    public abstract void Compose(IDocumentContainer container);
}

public class InstanceDocument(ModelService modelService, WorkflowInstance instance, Language language,
    byte[]? logo, ObjectContext context) : Document(language)
{
    public override void Compose(IDocumentContainer container)
    {
        var definition = modelService.WorkflowDefinitions[instance.WorkflowDefinition];
        container.Page(page =>
        {
            page.Margin(50);
            page.MarginTop(50);

            page.Header().Element(container =>
            {
                container.Column(column =>
                {
                    if (logo != null)
                        column.Item().Image(logo);
                    
                    column.Item().Height(100);
                    
                    column.Item().Text(Localize(definition.Title ?? definition.Name))
                        .Style(TextStyle.Default.FontSize(30));
                    column.Item().Height(10);
                    column.Item().Text(Localize(definition.InstanceTitleTemplate?.Execute(context) ?? ""))
                        .Style(TextStyle.Default.FontSize(20).Bold());

                    column.Item().Height(30);
                    
                    column.Item().Text($"{Localize("Exported", "Geëxporteerd")}: {DateTime.Now:d MMMM yyyy}");
                    column.Item().Height(5);
                    // TODO: show step?
                    //column.Item().Text($"Status: {Localize(state?.DisplayName ?? new("Draft", "Concept"))}");
                });
            });
        });
    }
}