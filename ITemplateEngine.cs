namespace HtmlTemplateEngine
{
    public interface ITemplateEngine
    {
        string Execute(string templateName, object model = null);
    }
}