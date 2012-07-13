﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autofac;
using System.Reflection;
using Lucene.Net.Orm;
using Lucene.Net.Orm.Mappers;
using System.IO;
using FilesIndexer.Models;
using Lucene.Net.Store;
using System.Monads;
using Dir = Lucene.Net.Store.Directory;
using FileDir = System.IO.Directory;
using Lucene.Net.Index;
using Lucene.Net.Analysis.Standard;

namespace FilesIndexer
{
    /// <summary>
    /// Demo application. This application demonstrates basic principles of Object-Document Mapping (ODM).
    /// It is an analogue of ORM (Object relational mapping) for document-orientired database, like Lucene.
    /// </summary>
    class Program
    {
        // Limits for demo - we demonstrate principle only.
        /// <summary>
        /// Maximal files quantity for processing.
        /// </summary>
        const int MaxFilesCount = 1000;
        /// <summary>
        /// Maximal size of text in single file (in characters).
        /// </summary>
        const int MaxTextSize = 1024 * 1024;
        
        static IContainer _container;

        static void Main(string[] args)
        {
            Register();
            IndexFiles();
            SearchCycle();
        }

        /// <summary>
        /// Registration of used components in autofac container.
        /// </summary>
        private static void Register()
        {
            ContainerBuilder builder = new ContainerBuilder();

            builder.RegisterType<ReflectionDocumentMapper>().As<IDocumentMapper>().SingleInstance();

            builder.Register(c => new Lucene.Net.Orm.FluentMappingsService(Assembly.GetExecutingAssembly()) 
                { Mapper = c.Resolve<IDocumentMapper>() })
                .As<IMappingsService>().SingleInstance();
            builder.Register(c => new RAMDirectory()).As<Dir>().SingleInstance();

            _container = builder.Build();
        }

        /// <summary>
        /// Method indexes all files found in the Samples subdirectory.
        /// </summary>
        private static void IndexFiles()
        {
            string pathForIndexing = new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath;
            pathForIndexing = Path.Combine(Path.GetDirectoryName(pathForIndexing), "Samples");
            IndexWriter writer = new IndexWriter(_container.Resolve<Dir>(), 
                new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_29),
                 IndexWriter.MaxFieldLength.UNLIMITED);
            IMappingsService mapper = _container.Resolve<IMappingsService>();

            IEnumerable<FileItem> itemsToIndexing = ItemsInPath(pathForIndexing);
            foreach (FileItem fi in itemsToIndexing)
            {
                fi.Do(ii => writer.AddDocument(mapper.GetDocument(ii)));
            }
            writer.Commit();
            writer.Close();
        }

        /// <summary>
        /// Method returns collection of document's found in passed directory.
        /// </summary>
        /// <param name="pathForIndexing">The path where text files will be searched.</param>
        /// <returns>FileItems collection. Some elements can be Null (if some error found).</returns>
        private static IEnumerable<FileItem> ItemsInPath(string pathForIndexing)
        {
            IEnumerable<string> names = FileDir.GetFiles(pathForIndexing, "*.*", SearchOption.AllDirectories)
                .Take(MaxFilesCount);
            foreach (string name in names)
            {
                FileItem item = null;
                try
                {
                    FileInfo fi = new FileInfo(name);
                    MetaInfo mi = new MetaInfo
                    {
                        CreationTime = fi.CreationTime,
                        ModificationTime = fi.LastWriteTime,
                        Readonly = fi.IsReadOnly,
                        Size = fi.Length,
                    };
                    string[] lines = null;
                    if (string.Compare(Path.GetExtension(name), "txt", true) == 0)
                    {
                        string text = File.ReadAllText(name).Substring(0, MaxTextSize);
                        lines = text.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
                    }
                    item = new FileItem
                    {
                        Filename = name,
                        MetaInfo = mi,
                        Text = lines,
                    };
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Exception while processing file {0}: {1}", name, ex);
                }
                yield return item;
            }            
        }

        static string[] exitCommands = { "quit", "exit" };

        /// <summary>
        /// Method realizes interaction with user.
        /// </summary>
        private static void SearchCycle()
        {
            Console.WriteLine("Indexing done. Please, enter search query. 'quit' or 'exit' - to finish application");
            bool exitFl = false;
            while (!exitFl)
            {
                Console.Write(">");
                string query = Console.ReadLine();
                foreach (string command in exitCommands)
                {
                    if (string.Compare(query, command, true) == 0)
                    {
                        exitFl = true;
                        break;
                    }
                }
            }
        }
    }
}
