// Converted from src/engine/DaRQ/Compiler/ResourceProvider.ts
using System;
using System.Collections.Generic;

namespace MiMFa.DaRQ.Compiler
{
    public interface IResourceEntry
    {
        string Name { get; }
        string Path { get; }
        bool IsFile { get; }
        bool IsFolder { get; }
    }

    public interface IResourceProvider
    {
        bool Exists(string path);
        bool IsAbsolute(string path);
        string Normalize(string path);
        string Resolve(string path, string? basePath = null);
        string Combine(params string[] paths);
        string DirectoryName(string path);
        string FileName(string path);
        string Extension(string path);
        IEnumerable<IResourceEntry> GetFolderContents(string path, bool recursive = false);
        string GetFileContents(string path, System.Text.Encoding encoding);
        void SetFileContents(string path, string content);
        void Delete(string path);
    }

    public class ResourceProvider : IResourceProvider
    {
        public virtual bool Exists(string path) => throw new NotSupportedException("The compiler does not support file systems in this provider.");
        public virtual bool IsAbsolute(string path) => throw new NotSupportedException();
        public virtual string Normalize(string path) => throw new NotSupportedException();
        public virtual string Resolve(string path, string? basePath = null) => throw new NotSupportedException();
        public virtual string Combine(params string[] paths) => throw new NotSupportedException();
        public virtual string DirectoryName(string path) => throw new NotSupportedException();
        public virtual string FileName(string path) => throw new NotSupportedException();
        public virtual string Extension(string path) => throw new NotSupportedException();
        public virtual IEnumerable<IResourceEntry> GetFolderContents(string path, bool recursive = false) => throw new NotSupportedException();
        public virtual string GetFileContents(string path, System.Text.Encoding encoding) => throw new NotSupportedException();
        public virtual void SetFileContents(string path, string content) => throw new NotSupportedException();
        public virtual void Delete(string path) => throw new NotSupportedException();
    }
}
