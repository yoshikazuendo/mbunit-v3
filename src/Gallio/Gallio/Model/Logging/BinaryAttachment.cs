// Copyright 2005-2008 Gallio Project - http://www.gallio.org/
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

namespace Gallio.Model.Logging
{
    /// <summary>
    /// Represents a binary-encoded attachments.
    /// </summary>
    [Serializable]
    public sealed class BinaryAttachment : Attachment
    {
        private readonly byte[] bytes;

        /// <summary>
        /// Creates an attachment.
        /// </summary>
        /// <param name="name">The attachment name, not null</param>
        /// <param name="contentType">The content type, not null</param>
        /// <param name="bytes">The binary data, not null</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="contentType"/> or <paramref name="bytes"/> is null</exception>
        public BinaryAttachment(string name, string contentType, byte[] bytes)
            : base(name, contentType)
        {
            if (bytes == null)
                throw new ArgumentNullException("bytes");

            this.bytes = bytes;
        }

        /// <summary>
        /// Gets the binary content of the attachment, not null.
        /// </summary>
        public byte[] Bytes
        {
            get { return bytes; }
        }

        /// <inheritdoc />
        public override AttachmentData ToAttachmentData()
        {
            return new AttachmentData(Name, ContentType, AttachmentEncoding.Base64, null, bytes);
        }
    }
}