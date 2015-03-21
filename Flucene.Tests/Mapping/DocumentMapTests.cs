﻿using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Lucene.Net.Odm.Mapping;
using System.Reflection;
using Lucene.Net.Odm.Mapping.Members;
using Lucene.Net.Orm.Tests.Helpers;
using NUnit.Framework;
using NSubstitute;


namespace Lucene.Net.Orm.Tests.DocumentMapTests
{
    [TestFixture]
    public abstract class DocumentMapTestBase<TMap, TModel>
        where TMap : DocumentMap<TModel>, new()
        where TModel : new() 
    {
        [Test]
        public virtual void GetMapping()
        {
            TMap map = new TMap();

            //action
            DocumentMapping<TModel> actual = map.GetMapping();

            DocumentMapping<TModel> expected = new DocumentMapping<TModel>();
            BuildExpectedModelMapping(expected);

            Test(expected, actual);
        }

        protected abstract void BuildExpectedModelMapping(DocumentMapping<TModel> mapping);


        private void Test(DocumentMapping<TModel> expected, DocumentMapping<TModel> actual)
        {
            Assert.AreEqual(expected.Fields.Count, actual.Fields.Count);

            IEnumerator<FieldMapping> expField, actField;
            for (expField = expected.Fields.GetEnumerator(), actField = actual.Fields.GetEnumerator(); expField.MoveNext() && actField.MoveNext();)
            {
                AssertHelper.FieldAssert(expField.Current, actField.Current);
            }

            Assert.AreEqual(expected.Embedded.Count, actual.Embedded.Count);

            IEnumerator<EmbeddedMapping> expEmb, actEmb;
            for (expEmb = expected.Embedded.GetEnumerator(), actEmb = actual.Embedded.GetEnumerator(); expEmb.MoveNext() && actEmb.MoveNext(); )
            {
                AssertHelper.EmbededAssert(expEmb.Current, actEmb.Current);
            }

            Assert.AreEqual(expected.CustomActions.Count, actual.CustomActions.Count);

            Assert.AreEqual(expected.Boost, actual.Boost);
        }

        #region Helpers

        protected PropertyInfo GetProperty(string name)
        {
            return typeof(TModel).GetProperty(name);
        }

        protected Member GetCustomMember(Type memberType)
        {
            var member = Substitute.For<Member>();
            member.CanWrite.Returns(memberType != null);
            member.MemberType.Returns(memberType);

            return member;
        }

        #endregion
    }

    public class PropertyMapDocumentMapTests : DocumentMapTestBase<PropertyMapDocumentMapTests.ModelMap, PropertyMapDocumentMapTests.Model>
    {
        protected override void BuildExpectedModelMapping(DocumentMapping<Model> mapping)
        {
            mapping.Fields.Add(new FieldMapping("Field", new PropertyMember(GetProperty("Field"))));
        }

        public class Model
        {
            public int Field { get; set; }
        }

        public class ModelMap : DocumentMap<Model>
        {
            public ModelMap()
            {
                Map(x => x.Field);
            }
        }
    }

    public class CustomFieldDocumentMapTests : DocumentMapTestBase<CustomFieldDocumentMapTests.ModelMap, CustomFieldDocumentMapTests.Model>
    {
        protected override void BuildExpectedModelMapping(DocumentMapping<Model> mapping)
        {
            mapping.Fields.Add(new FieldMapping("CustomField", GetCustomMember(null)));
        }


        public class Model
        {
            public string Text { get; set; }
        }

        public class ModelMap : DocumentMap<Model>
        {
            public ModelMap()
            {
                Map(x => x.Text.ToLowerInvariant(), "CustomField");
            }
        }
    }

    public class CustomMapDocumentMapTests : DocumentMapTestBase<CustomMapDocumentMapTests.ModelMap, CustomMapDocumentMapTests.Model>
    {
        protected override void BuildExpectedModelMapping(DocumentMapping<Model> mapping)
        {
            mapping.Fields.Add(new FieldMapping("version", GetCustomMember(typeof(IEnumerable<string>))));
        }

        public class Model
        {
            public Version Version { get; set; }
        }

        public class ModelMap : DocumentMap<Model>
        {
            public ModelMap()
            {
                Map(x => x.Version.ToString(), (x, v) => x.Version = Version.Parse(v.First()), "version");
            }
        }
    }

    public class EmbededDocumentMapTests : DocumentMapTestBase<EmbededDocumentMapTests.ModelMap, EmbededDocumentMapTests.Model>
    {
        protected override void BuildExpectedModelMapping(DocumentMapping<Model> mapping)
        {
            mapping.Embedded.Add(new EmbeddedMapping(new PropertyMember(GetProperty("EmbededModel")))
            {
                Prefix = "Embeded_"
            });
        }


        public class Model
        {
            public SubModel EmbededModel { get; set; }
        }

        public class SubModel
        {
        }

        public class ModelMap : DocumentMap<Model>
        {
            public ModelMap()
            {
                Embedded(x => x.EmbededModel).Prefix("Embeded_");
            }
        }
    }

    public class InvalidCustomFieldDocumentMapTests : DocumentMapTestBase<InvalidCustomFieldDocumentMapTests.ModelMap, InvalidCustomFieldDocumentMapTests.Model>
    {
        [Test]
        //[ExpectedException(typeof(InvalidOperationException))]
        public override void GetMapping()
        {
            base.GetMapping();
        }

        protected override void BuildExpectedModelMapping(DocumentMapping<Model> mapping)
        {
            mapping.Fields.Add(new FieldMapping(null, GetCustomMember(null)));
        }

        public class Model
        {
            public string Title { get; set; }
        }

        public class ModelMap : DocumentMap<Model>
        {
            public ModelMap()
            {
                Map(x => x.Title.ToUpperInvariant());
            }
        }
    }

    public class PropertyCustomNameMapDocumentMapTests : DocumentMapTestBase<PropertyCustomNameMapDocumentMapTests.ModelMap, PropertyCustomNameMapDocumentMapTests.Model>
    {
        protected override void BuildExpectedModelMapping(DocumentMapping<Model> mapping)
        {
            mapping.Fields.Add(new FieldMapping("CustomName", new PropertyMember(GetProperty("Property"))));
        }


        public class Model
        {
            public string Property { get; set; }
        }

        public class ModelMap : DocumentMap<Model>
        {
            public ModelMap()
            {
                Map(x => x.Property, "CustomName");
            }
        }
    }
}
