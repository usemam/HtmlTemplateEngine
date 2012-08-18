using System;
using System.Diagnostics;
using System.Text;

namespace HtmlTemplateEngine
{
    public abstract class HtmlTemplate : ITemplate
    {
        private readonly StringBuilder buffer;

        [DebuggerStepThrough]
        protected HtmlTemplate()
        {
            buffer = new StringBuilder();
        }

        public string Body
        {
            get { return buffer.ToString(); }
        }

        protected dynamic Model { get; private set; }

        public void SetModel(dynamic model)
        {
            Model = model;
        }

        public abstract void Execute();

        public virtual void Write(object value)
        {
            WriteLiteral(value);
        }

        public virtual void WriteLiteral(object value)
        {
            buffer.Append(value);
        }

        public Func<string, object, string> RenderPartialImpl;

        public string RenderPartial(string templateName, object model = null)
        {
            return RenderPartialImpl(templateName, model);
        }
    }
}
