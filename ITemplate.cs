namespace HtmlTemplateEngine
{
    public interface ITemplate
    {
        string Body { get; }

        void SetModel(dynamic model);

        void Execute();
    }
}