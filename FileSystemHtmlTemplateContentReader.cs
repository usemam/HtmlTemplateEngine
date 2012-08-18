﻿using System;
using System.Globalization;
using System.IO;

namespace HtmlTemplateEngine
{
    public class FileSystemHtmlTemplateContentReader : ITemplateContentReader
    {
        protected string TemplateDirectory { get; private set; }

        protected string FileExtension { get; private set; }

        public FileSystemHtmlTemplateContentReader() : this("templates", ".cshtml")
        {
        }

        public FileSystemHtmlTemplateContentReader(string templateDirectory, string fileExtension)
        {
            Invariant.IsNotBlank(templateDirectory, "templateDirectory");

            if (!Path.IsPathRooted(templateDirectory))
            {
                templateDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, templateDirectory);
            }

            if (!Directory.Exists(templateDirectory))
            {
                throw new DirectoryNotFoundException(string.Format(CultureInfo.CurrentCulture, "\"{0}\" does not exist.", templateDirectory));
            }

            TemplateDirectory = templateDirectory;
            FileExtension = fileExtension;
        }

        public string Read(string templateName, string suffix)
        {
            Invariant.IsNotBlank(templateName, "templateName");

            var content = string.Empty;
            var path = BuildPath(templateName, suffix);

            if (File.Exists(path))
            {
                content = File.ReadAllText(path);
            }

            return content;
        }

        protected virtual string BuildPath(string templateName, string suffix)
        {
            var fileName = templateName;

            if (!string.IsNullOrWhiteSpace(suffix))
            {
                fileName += "." + suffix;
            }

            if (!string.IsNullOrWhiteSpace(FileExtension))
            {
                fileName += FileExtension;
            }

            var path = Path.Combine(TemplateDirectory, fileName);

            return path;
        }
    }
}
