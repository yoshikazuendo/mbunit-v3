// Copyright 2007 MbUnit Project - http://www.mbunit.com/
// Portions Copyright 2000-2004 Jonathan De Halleux, Jamie Cansdale
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

extern alias MbUnit2;

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Gallio.Hosting;
using Gallio.Model.Data;
using Gallio.Model;
using Gallio.Model.Reflection;
using TestFixturePatternAttribute2 = MbUnit2::MbUnit.Core.Framework.TestFixturePatternAttribute;
using TestPatternAttribute2 = MbUnit2::MbUnit.Core.Framework.TestPatternAttribute;
using FixtureCategoryAttribute2 = MbUnit2::MbUnit.Framework.FixtureCategoryAttribute;
using TestCategoryAttribute2 = MbUnit2::MbUnit.Framework.TestCategoryAttribute;
using TestImportance2 = MbUnit2::MbUnit.Framework.TestImportance;
using AuthorAttribute2 = MbUnit2::MbUnit.Framework.AuthorAttribute;
using TestsOnAttribute2 = MbUnit2::MbUnit.Framework.TestsOnAttribute;
using ImportanceAttribute2 = MbUnit2::MbUnit.Framework.ImportanceAttribute;
using MbUnit2::MbUnit.Core;
using MbUnit2::MbUnit.Core.Remoting;
using MbUnit2::MbUnit.Core.Filters;
using MbUnit2::MbUnit.Core.Reports.Serialization;
using MbUnit2::MbUnit.Core.Invokers;
using Gallio.Plugin.MbUnit2Adapter.Properties;

namespace Gallio.Plugin.MbUnit2Adapter.Model
{
    /// <summary>
    /// The MbUnit v2 test assembly template binding.
    /// This binding performs full exploration of all tests in MbUnit v2 test
    /// assemblies during test construction.
    /// </summary>
    public class MbUnit2AssemblyTemplateBinding : BaseTemplateBinding
    {
        private const string MbUnitAssemblyNamePrefix = "MbUnit2.AssemblyName:";
        private FixtureExplorer fixtureExplorer;

        /// <summary>
        /// Creates a template binding.
        /// </summary>
        /// <param name="template">The template that was bound</param>
        /// <param name="scope">The scope in which the binding occurred</param>
        /// <param name="arguments">The template arguments</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="template"/>,
        /// <paramref name="scope"/> or <paramref name="arguments"/> is null</exception>
        public MbUnit2AssemblyTemplateBinding(MbUnit2AssemblyTemplate template, TemplateBindingScope scope,
            IDictionary<ITemplateParameter, IDataFactory> arguments)
            : base(template, scope, arguments)
        {
        }

        /// <summary>
        /// Gets the assembly.
        /// </summary>
        public IAssemblyInfo Assembly
        {
            get { return ((MbUnit2AssemblyTemplate) Template).Assembly; }
        }

        /// <summary>
        /// Gets the fixture explorer.
        /// </summary>
        public FixtureExplorer FixtureExplorer
        {
            get
            {
                RunFixtureExplorerIfNeeded();
                return fixtureExplorer;
            }
        }

        /// <inheritdoc />
        public override void BuildTests(TestTreeBuilder builder, ITest parent)
        {
            RunFixtureExplorerIfNeeded();

            MbUnit2Test assemblyTest = CreateAssemblyTest(parent, Assembly);

            foreach (Fixture fixture in fixtureExplorer.FixtureGraph.Fixtures)
            {
                MbUnit2Test fixtureTest = CreateFixtureTest(assemblyTest, fixture);

                foreach (RunPipeStarter starter in fixture.Starters)
                {
                    CreateTest(fixtureTest, starter.Pipe);
                }
            }

            // Register the root assembly test so that dependencies can be resolved later.
            builder.RegisterNode(MbUnitAssemblyNamePrefix + fixtureExplorer.AssemblyName, assemblyTest);
            builder.ResolveReferences += delegate
            {
                foreach (string assemblyName in fixtureExplorer.GetDependentAssemblies())
                {
                    ITest test = builder.GetNode(MbUnitAssemblyNamePrefix + assemblyName);
                    if (test != null)
                        assemblyTest.Dependencies.Add(test);
                }
            };
        }

        private void RunFixtureExplorerIfNeeded()
        {
            if (fixtureExplorer != null)
                return;

            try
            {
                fixtureExplorer = new FixtureExplorer(Assembly.Resolve());
                fixtureExplorer.Filter = new AnyFixtureFilter();
                fixtureExplorer.Explore();

                AnyRunPipeFilter runPipeFilter = new AnyRunPipeFilter();
                foreach (Fixture fixture in fixtureExplorer.FixtureGraph.Fixtures)
                    fixture.Load(runPipeFilter);
            }
            catch (Exception)
            {
                fixtureExplorer = null;
                throw;
            }
        }

        private MbUnit2Test CreateAssemblyTest(ITest parent, IAssemblyInfo assembly)
        {
            MbUnit2Test test = new MbUnit2Test(assembly.GetName().Name, assembly, this, null, null);
            test.Kind = ComponentKind.Assembly;

            parent.AddChild(test);
            return test;
        }

        private MbUnit2Test CreateFixtureTest(ITest parent, Fixture fixture)
        {
            ITypeInfo fixtureType = Reflector.Wrap(fixture.Type);
            MbUnit2Test test = new MbUnit2Test(fixtureType.CompoundName, fixtureType, this, fixture, null);
            test.Kind = ComponentKind.Fixture;

            // Populate metadata
            foreach (AuthorAttribute2 attrib in AttributeUtils.GetAttributes<AuthorAttribute2>(fixtureType, true))
            {
                if (! String.IsNullOrEmpty(attrib.Name))
                    test.Metadata.Add(MetadataKeys.AuthorName, attrib.Name);
                if (! String.IsNullOrEmpty(attrib.EMail) && attrib.EMail != @"unspecified")
                    test.Metadata.Add(MetadataKeys.AuthorEmail, attrib.EMail);
                if (!String.IsNullOrEmpty(attrib.HomePage) && attrib.HomePage != @"unspecified")
                    test.Metadata.Add(MetadataKeys.AuthorHomepage, attrib.HomePage);
            }
            foreach (FixtureCategoryAttribute2 attrib in AttributeUtils.GetAttributes<FixtureCategoryAttribute2>(fixtureType, true))
            {
                test.Metadata.Add(MetadataKeys.CategoryName, attrib.Category);
            }
            foreach (TestsOnAttribute2 attrib in AttributeUtils.GetAttributes<TestsOnAttribute2>(fixtureType, true))
            {
                test.Metadata.Add(MetadataKeys.TestsOn, attrib.TestedType.AssemblyQualifiedName);
            }
            foreach (ImportanceAttribute2 attrib in AttributeUtils.GetAttributes<ImportanceAttribute2>(fixtureType, true))
            {
                test.Metadata.Add(MetadataKeys.Importance, attrib.Importance.ToString());
            }
            foreach (TestFixturePatternAttribute2 attrib in AttributeUtils.GetAttributes<TestFixturePatternAttribute2>(fixtureType, true))
            {
                if (! String.IsNullOrEmpty(attrib.Description))
                    test.Metadata.Add(MetadataKeys.Description, attrib.Description);
            }

            string xmlDocumentation = fixtureType.GetXmlDocumentation();
            if (xmlDocumentation != null)
                test.Metadata.Add(MetadataKeys.XmlDocumentation, xmlDocumentation);

            parent.AddChild(test);
            return test;
        }

        private void CreateTest(ITest parent, RunPipe runPipe)
        {
            IMemberInfo member = GuessMemberInfoFromRunPipe(runPipe);
            ICodeElementInfo codeElement = member ?? Reflector.Wrap(runPipe.FixtureType);

            MbUnit2Test test = new MbUnit2Test(runPipe.Name, codeElement, this, runPipe.Fixture, runPipe);
            test.Kind = ComponentKind.Test;
            test.IsTestCase = true;

            // Populate metadata
            if (member != null)
            {
                foreach (TestPatternAttribute2 attrib in AttributeUtils.GetAttributes<TestPatternAttribute2>(member, true))
                {
                    if (!String.IsNullOrEmpty(attrib.Description))
                        test.Metadata.Add(MetadataKeys.Description, attrib.Description);
                }

                string xmlDocumentation = member.GetXmlDocumentation();
                if (xmlDocumentation != null)
                    test.Metadata.Add(MetadataKeys.XmlDocumentation, xmlDocumentation);
            }

            parent.AddChild(test);
        }

        /// <summary>
        /// MbUnit v2 does not expose the MemberInfo directly.  Arguably
        /// that allows more general filtering rules than Gallio's simple
        /// CodeReference but it is a bit of a nuisance for us here.
        /// So to avoid breaking the MbUnit v2 API, we resort to a
        /// hack based on guessing the right method.
        /// </summary>
        private IMemberInfo GuessMemberInfoFromRunPipe(RunPipe runPipe)
        {
            foreach (RunInvokerVertex vertex in runPipe.Invokers)
            {
                if (! vertex.HasInvoker)
                    continue;

                IRunInvoker invoker = vertex.Invoker;
                if (invoker.Generator.IsTest)
                {
                    // Note: This is the hack.
                    //       We assume the run invoker's name matches the name of
                    //       the actual member and that the member is public and is
                    //       declared by the fixture type.  That should be true with
                    //       all built-in MbUnit v2 invokers.  -- Jeff.
                    Type fixtureType = runPipe.FixtureType;
                    string probableMemberName = invoker.Name;

                    // Strip off arguments from a RowTest's member name.  eg. FooMember(123, 456)
                    int parenthesis = probableMemberName.IndexOf('(');
                    if (parenthesis >= 0)
                        probableMemberName = probableMemberName.Substring(0, parenthesis);

                    foreach (MemberInfo member in fixtureType.GetMember(probableMemberName,
                        BindingFlags.Public | BindingFlags.Instance))
                    {
                        if (invoker.ContainsMemberInfo(member))
                            return Reflector.Wrap(member);
                    }
                }
            }

            return null;
        }
    }
}
