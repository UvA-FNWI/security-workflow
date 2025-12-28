using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuestPDF.Markdown;
using UvA.Workflow.Entities.Domain;
using UvA.Workflow.Tools;
using UvA.Workflow.Users;
using UvA.Workflow.WorkflowInstances;

namespace UvA.Workflow.Security.Pdf;

public class FormDocument(WorkflowInstance instance, Form form, Language language) : Document(language)
{
    public override void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Margin(50);
            page.Header().Element(ComposeHeader);
            page.Content().Element(ComposeContent);
        });
    }

    private void ComposeHeader(IContainer container)
    {
        var instanceEvent = instance.Events[form.Name];
        container.Column(column =>
        {
            column.Item().Text(Localize(form.DisplayName)).Style(TextStyle.Default.FontSize(20).SemiBold());
            column.Item().Text($"{Localize("Submitted", "Ingediend")}: {instanceEvent.Date:d MMMM yyyy}");
            column.Item().Height(15);
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(1);
                columns.ConstantColumn(5);
                columns.RelativeColumn(3);
            });
            
            table.Header(header =>
            {
                header.Cell().Element(CellStyle).Text(Localize("Question", "Vraag"));
                header.Cell();
                header.Cell().Element(CellStyle).Text(Localize("Answer", "Antwoord"));

                static IContainer CellStyle(IContainer container)
                    => container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1)
                        .BorderColor(Colors.Black);
            });
            
            foreach (var question in form.PropertyDefinitions
                         .Where(q => !q.HideInResults) 
                         .Where(q => instance.HasAnswer(q.Name)))
            {
                table.Cell().Element(CellStyle).Column(c =>
                {
                    c.Item().Text(Localize(question.ShortDisplayName));
                    if (question.Description != null)
                    {
                        c.Item().Height(4);
                        c.Item().Text(Localize(question.Description)).FontSize(8);
                    }
                });
                table.Cell();
                var answer = ObjectContext.GetValue(instance.Properties[question.Name], question);
                if (question.Layout?.GetValueOrDefault("multiline") is true && answer is string s)
                    table.Cell().Element(CellStyle).Markdown(s);
                else if (question.DataType == DataType.Choice && answer is string choice)
                    table.Cell().Element(CellStyle).Text(Localize(question.Values?.GetValueOrDefault(choice)?.Text ?? ""));
                else if (question.DataType == DataType.Choice && answer is string[] choices)
                    table.Cell().Element(CellStyle).Text(choices
                        .Select(c => Localize(question.Values?.GetValueOrDefault(c)?.Text ?? ""))
                        .ToSeparatedString());
                else
                    table.Cell().Element(CellStyle).Text(answer switch
                    {
                        DateTime d => d.ToString("d MMM yyyy"),
                        User u => u.DisplayName,
                        User[] us => us.ToSeparatedString(u => u.DisplayName),
                        string[] ss => ss.ToSeparatedString(),
                        Dictionary<string, object>[] docs => docs.ToSeparatedString(d => d.Values.ToSeparatedString(), "\n"), 
                        _ => answer?.ToString()
                    });
                
                static IContainer CellStyle(IContainer container)
                    => container.PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten2);
            }
        });
    }
}