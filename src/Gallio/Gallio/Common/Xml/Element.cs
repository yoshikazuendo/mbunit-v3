﻿// Copyright 2005-2009 Gallio Project - http://www.gallio.org/
// Portions Copyright 2000-2004 Jonathan de Halleux
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

using System;
using System.Collections.Generic;
using System.Text;

namespace Gallio.Common.Xml
{
    /// <summary>
    /// Represents an element in an XML fragment.
    /// </summary>
    public class Element : Node, IDiffable<Element>, INamed
    {
        private readonly string name;
        private readonly string value;
        private readonly AttributeCollection attributes;

        /// <inheritdoc />
        public string Name
        {
            get
            {
                return name;
            }
        }

        /// <summary>
        /// Gets the literal value of the element.
        /// </summary>
        public string Value
        {
            get
            {
                return value;
            }
        }

        /// <summary>
        /// Gets the attributes of the element.
        /// </summary>
        public AttributeCollection Attributes
        {
            get
            {
                return attributes;
            }
        }

        /// <summary>
        /// Constructs an XML element.
        /// </summary>
        /// <param name="child">The child node of the element (usually an <see cref="Element"/> or an <see cref="ElementCollection"/>)</param>
        /// <param name="name">The name of the element.</param>
        /// <param name="value">The value of the element.</param>
        /// <param name="attributes">The attributes of the element.</param>
        public Element(INode child, string name, string value, AttributeCollection attributes)
            : base(child)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            if (value == null)
                throw new ArgumentNullException("value");
            if (attributes == null)
                throw new ArgumentNullException("attributes");

            this.name = name;
            this.value = value;
            this.attributes = attributes;
        }

        /// <inheritdoc />
        public override string ToXml()
        {
            if (!Child.IsNull)
            {
                return String.Format("<{0}{1}>{2}</{0}>", name, attributes.ToXml(), Child.ToXml());
            }
            else if (value.Length == 0)
            {
                return String.Format("<{0}{1}/>", name, attributes.ToXml());
            }
            else
            {
                return String.Format("<{0}{1}>{2}</{0}>", name, attributes.ToXml(), value);
            }
        }

        /// <inheritdoc />
        public override DiffSet Diff(INode expected, IXmlPathOpen path, Options options)
        {
            return Diff((Element)expected, path, options);
        }

        /// <inheritdoc />
        public virtual DiffSet Diff(Element expected, IXmlPathOpen path, Options options)
        {
            if (expected == null)
                throw new ArgumentNullException("expected");
            if (path == null)
                throw new ArgumentNullException("path");

            var builder = new DiffSetBuilder();

            if (!AreNamesEqual(expected.Name, options))
            {
                builder.Add(new Diff(path.ToString(), "Unexpected element found.", expected.Name, name));
            }
            else
            {
                if (!value.Equals(expected.Value, GetComparisonTypeForValue(options)))
                {
                    builder.Add(new Diff(path.Element(name).ToString(), "Unexpected element value found.", expected.Value, value));
                }

                builder.Add(attributes.Diff(expected.Attributes, path.Element(name), options));
                builder.Add(Child.Diff(expected.Child, path.Element(name), options));
            }

            return builder.ToDiffSet();
        }

        private static StringComparison GetComparisonTypeForName(Options options)
        {
            return (((options & Options.IgnoreElementsNameCase) != 0)
                ? StringComparison.CurrentCultureIgnoreCase
                : StringComparison.CurrentCulture);
        }

        private static StringComparison GetComparisonTypeForValue(Options options)
        {
            return (((options & Options.IgnoreElementsValueCase) != 0)
                ? StringComparison.CurrentCultureIgnoreCase
                : StringComparison.CurrentCulture);
        }

        /// <inheritdoc />
        public bool AreNamesEqual(string otherName, Options options)
        {
            return name.Equals(otherName, GetComparisonTypeForName(options));
        }
    
        /// <inheritdoc />
        public override bool Contains(XmlPathClosed searchedItem, int depth)
        {
            if (searchedItem == null)
                throw new ArgumentNullException("searchedItem");
            if (depth < 0)
                throw new ArgumentOutOfRangeException("depth", "The depth must be greater than or equal to zero.");
        
            if (depth >= searchedItem.ElementNames.Count)
                return false;

            if (!searchedItem.ElementNames[depth].Equals(name))
                return false;

            if ((depth == searchedItem.ElementNames.Count - 1) &&
                (searchedItem.AttributeName != null) &&
                !attributes.Contains(searchedItem.AttributeName))
                return false;

            if (depth == searchedItem.ElementNames.Count - 1)
                return true;

            return Child.Contains(searchedItem, depth + 1);
        }
    }
}