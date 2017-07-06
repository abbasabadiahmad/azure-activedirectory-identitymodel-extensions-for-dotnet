//------------------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using Microsoft.IdentityModel.Tests;
using Xunit;

namespace Microsoft.IdentityModel.Xml.Tests
{
    public class SignedInfoTests
    {
#pragma warning disable CS3016 // Arrays as attribute arguments is not CLS-compliant
        [Theory, MemberData("SignedInfoConstructorTheoryData")]
#pragma warning restore CS3016 // Arrays as attribute arguments is not CLS-compliant
        public void SignedInfoConstructor(DSigTheoryData theoryData)
        {
            TestUtilities.WriteHeader($"{this}.SignedInfoConstructor", theoryData);
            List<string> errors = new List<string>();
            try
            {
                var signedInfo = new SignedInfo();
                if (signedInfo.Reference != null)
                    errors.Add("signedInfo.Reference != null");

                if (!string.IsNullOrEmpty(signedInfo.SignatureAlgorithm))
                    errors.Add("!string.IsNullOrEmpty(signedInfo.SignatureAlgorithm)");

                theoryData.ExpectedException.ProcessNoException();
            }
            catch (Exception ex)
            {
                theoryData.ExpectedException.ProcessException(ex);
            }

            TestUtilities.AssertFailIfErrors(errors);
        }

        public static TheoryData<DSigTheoryData> SignedInfoConstructorTheoryData
        {
            get
            {
                return new TheoryData<DSigTheoryData>
                {
                    new DSigTheoryData
                    {
                        First = true,
                        TestId = "Constructor"
                    }
                };
            }
        }

#pragma warning disable CS3016 // Arrays as attribute arguments is not CLS-compliant
        [Theory, MemberData("SignedInfoReadFromTheoryData")]
#pragma warning restore CS3016 // Arrays as attribute arguments is not CLS-compliant
        public void SignedInfoReadFrom(DSigTheoryData theoryData)
        {
            TestUtilities.WriteHeader($"{this}.SignedInfoReadFrom", theoryData);
            var context = new CompareContext($"{this}.SignedInfoReadFrom, {theoryData.TestId}");

            var errors = new List<string>();
            try
            {
                var sr = new StringReader(theoryData.SignedInfoTestSet.Xml);
                var reader = XmlDictionaryReader.CreateDictionaryReader(XmlReader.Create(sr));
                var signedInfo = new SignedInfo();
                signedInfo.ReadFrom(reader);
                theoryData.ExpectedException.ProcessNoException(context.Diffs);
                if (theoryData.ExpectedException.TypeExpected == null)
                    IdentityComparer.AreEqual(signedInfo, theoryData.SignedInfoTestSet.SignedInfo, context);
            }
            catch (Exception ex)
            {
                theoryData.ExpectedException.ProcessException(ex);
            }

            TestUtilities.AssertFailIfErrors(context);
        }

        public static TheoryData<DSigTheoryData> SignedInfoReadFromTheoryData
        {
            get
            {
                // uncomment to view exception displayed to user
                // ExpectedException.DefaultVerbose = true;

                return new TheoryData<DSigTheoryData>
                {
                    new DSigTheoryData
                    {
                        First = true,
                        SignedInfoTestSet = ReferenceXml.SignedInfoValid,
                        TestId = nameof(ReferenceXml.SignedInfoValid)
                    },
                    new DSigTheoryData
                    {
                        SignedInfoTestSet = ReferenceXml.SignInfoStartsWithWhiteSpace,
                        TestId = nameof(ReferenceXml.SignInfoStartsWithWhiteSpace),
                    },
                    new DSigTheoryData
                    {
                        ExpectedException = new ExpectedException(typeof(XmlReadException), "IDX21011:"),
                        SignedInfoTestSet = ReferenceXml.SignedInfoCanonicalizationMethodMissing,
                        TestId = nameof(ReferenceXml.SignedInfoCanonicalizationMethodMissing)
                    },
                    new DSigTheoryData
                    {
                        ExpectedException = new ExpectedException(typeof(XmlReadException), "IDX21011: Unable to read xml. Expecting XmlReader to be at ns.element: 'http://www.w3.org/2000/09/xmldsig#.Reference'"),
                        SignedInfoTestSet = ReferenceXml.SignedInfoReferenceMissing,
                        TestId = nameof(ReferenceXml.SignedInfoReferenceMissing)
                    },
                    new DSigTheoryData
                    {
                        ExpectedException = new ExpectedException(typeof(XmlReadException), "Unable to read xml. Expecting XmlReader to be at ns.element: 'http://www.w3.org/2000/09/xmldsig#.Transforms'"),
                        SignedInfoTestSet = ReferenceXml.SignedInfoTransformsMissing,
                        TestId = nameof(ReferenceXml.SignedInfoTransformsMissing)
                    },
                    new DSigTheoryData
                    {
                        ExpectedException = new ExpectedException(typeof(XmlReadException), "IDX21011: Unable to read xml. Expecting XmlReader to be at ns.element: 'http://www.w3.org/2000/09/xmldsig#.Transforms', "),
                        SignedInfoTestSet = ReferenceXml.SignedInfoNoTransforms,
                        TestId = nameof(ReferenceXml.SignedInfoNoTransforms)
                    },
                    new DSigTheoryData
                    {
                        ExpectedException = new ExpectedException(typeof(XmlException), "IDX21018: Unable to read xml. A Reference contains an unknown transform "),
                        SignedInfoTestSet = ReferenceXml.SignedInfoUnknownCanonicalizationtMethod,
                        TestId = nameof(ReferenceXml.SignedInfoUnknownCanonicalizationtMethod)
                    },
                    new DSigTheoryData
                    {
                        ExpectedException = new ExpectedException(typeof(XmlException), "IDX21018: Unable to read xml. A Reference contains an unknown transform "),
                        SignedInfoTestSet = ReferenceXml.SignedInfoUnknownTransform,
                        TestId = nameof(ReferenceXml.SignedInfoUnknownTransform)
                    },
                    new DSigTheoryData
                    {
                        ExpectedException = new ExpectedException(typeof(XmlReadException), "IDX21011: Unable to read xml. Expecting XmlReader to be at ns.element: 'http://www.w3.org/2000/09/xmldsig#.DigestMethod', "),
                        SignedInfoTestSet = ReferenceXml.SignedInfoMissingDigestMethod,
                        TestId = nameof(ReferenceXml.SignedInfoMissingDigestMethod)
                    },
                    new DSigTheoryData
                    {
                        ExpectedException = new ExpectedException(typeof(XmlReadException), "IDX21011: Unable to read xml. Expecting XmlReader to be at ns.element: 'http://www.w3.org/2000/09/xmldsig#.DigestValue', "),
                        SignedInfoTestSet = ReferenceXml.SignedInfoMissingDigestValue,
                        TestId = nameof(ReferenceXml.SignedInfoMissingDigestValue)
                    }
                };
            }
        }
    }
}