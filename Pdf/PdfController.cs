using Microsoft.AspNetCore.Mvc;
using UvA.Workflow.Entities.Domain;
using UvA.Workflow.Users;
using UvA.Workflow.WorkflowInstances;

namespace UvA.Workflow.Security.Pdf;

[ApiController]
[Route("[controller]")]
public class PdfController(IWorkflowInstanceRepository repository, PdfService pdfService,
    RightsService rightsService, TokenService tokenService) : ControllerBase
{
    private const string InstanceType = "__Instance";
    
    [HttpGet("{instanceId}")]
    public async Task<ActionResult<LinkInfo>> GetPdfLinks(string instanceId, CancellationToken ct)
    {
        var inst = await repository.GetById(instanceId, ct);
        if (inst == null) return NotFound();
        
        var allowed = (await rightsService.GetAllowedActions(inst, RoleAction.View)).SelectMany(r => r.AllForms)
            .Distinct().ToArray();

        return new LinkInfo(
            tokenService.GenerateVerifier(instanceId, InstanceType, allowed),
            allowed.ToDictionary(f => f, f => tokenService.GenerateVerifier(instanceId, f))
        );
    }
    
    [HttpGet("{instanceId}/Document")]
    public async Task<ActionResult> GetDocument(string instanceId,
        [FromQuery] string token, CancellationToken ct,
        [FromQuery] Language language = Language.En)
    {
        if (!await tokenService.IsValid(instanceId, token, InstanceType))
            return Forbid();

        var forms = await tokenService.GetAllowedForms(token);

        var content = await pdfService.GenerateInstancePdf(instanceId, forms, language, ct);
        return File(content, "application/pdf");
    }
    
    [HttpGet("{instanceId}/Forms/{formName}")]
    public async Task<ActionResult> GetDocument(string instanceId, string formName,
        [FromQuery] string token, CancellationToken ct,
        [FromQuery] Language language = Language.En)
    {
        if (!await tokenService.IsValid(instanceId, token, formName))
            return Forbid();

        var content = await pdfService.GenerateSubmissionPdf(instanceId, formName, language, ct);
        return File(content, "application/pdf");
    }
    
}

public record LinkInfo(string Token, Dictionary<string, string> FormTokens);