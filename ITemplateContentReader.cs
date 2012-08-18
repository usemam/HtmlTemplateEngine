namespace HtmlTemplateEngine
{
    public interface ITemplateContentReader
    {
        string Read(string templateName, string suffix);
    }
}