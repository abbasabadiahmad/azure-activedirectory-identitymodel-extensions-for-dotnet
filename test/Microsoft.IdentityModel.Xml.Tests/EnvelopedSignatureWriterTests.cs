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
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml;
using Microsoft.IdentityModel.Protocols.WsFederation;
using Microsoft.IdentityModel.TestUtils;
using Microsoft.IdentityModel.Tokens.Saml;
using Microsoft.IdentityModel.Tokens.Saml2;
using Microsoft.IdentityModel.Xml;
using Xunit;

#pragma warning disable CS3016 // Arrays as attribute arguments is not CLS-compliant

namespace Microsoft.IdentityModel.Tokens.Xml.Tests
{
    public class EnvelopedSignatureWriterTests
    {
        [Theory, MemberData(nameof(ConstructorTheoryData))]
        public void Constructor(EnvelopedSignatureTheoryData theoryData)
        {
            TestUtilities.WriteHeader($"{this}.Constructor", theoryData);
            try
            {
                var envelopedWriter = new EnvelopedSignatureWriter(theoryData.XmlWriter, theoryData.SigningCredentials, theoryData.ReferenceId, theoryData.InclusiveNamespacesPrefixList);
                theoryData.ExpectedException.ProcessNoException();
            }
            catch (Exception ex)
            {
                theoryData.ExpectedException.ProcessException(ex);
            }
        }

        public static TheoryData<EnvelopedSignatureTheoryData> ConstructorTheoryData
        {
            get
            {
                return new TheoryData<EnvelopedSignatureTheoryData>
                {
                    new EnvelopedSignatureTheoryData
                    {
                        ExpectedException = ExpectedException.ArgumentNullException("IDX10000:"),
                        First = true,
                        ReferenceId = null,
                        SigningCredentials = null,
                        TestId = "Null XmlWriter",
                        XmlWriter = null
                    },
                    new EnvelopedSignatureTheoryData
                    {
                        ExpectedException = ExpectedException.ArgumentNullException("IDX10000:"),
                        ReferenceId = Guid.NewGuid().ToString(),
                        SigningCredentials = null,
                        TestId = "Null SigningCredentials",
                        XmlWriter = null
                    }
                };
            }
        }

        [Theory, MemberData(nameof(RoundTripSaml2TheoryData))]
        public void RoundTripSaml2(EnvelopedSignatureTheoryData theoryData)
        {
            var context = TestUtilities.WriteHeader($"{this}.RoundTripSaml2", theoryData);
            context.PropertiesToIgnoreWhenComparing.Add(typeof(Saml2Assertion), new List<string> { "CanonicalString" });

            try
            {
                var serializer = new Saml2Serializer(new EncryptedAssertionHandler());
                var samlAssertion = serializer.ReadAssertion(XmlUtilities.CreateDictionaryReader(theoryData.Xml));
                var stream = new MemoryStream();
                var writer = XmlDictionaryWriter.CreateTextWriter(stream);
                samlAssertion.SigningCredentials = theoryData.SigningCredentials;
                serializer.WriteAssertion(writer, samlAssertion);
                writer.Flush();
                var xml = Encoding.UTF8.GetString(stream.ToArray());
                samlAssertion.SigningCredentials = null;
                var samlAssertion2 = serializer.ReadAssertion(XmlUtilities.CreateDictionaryReader(xml));
                samlAssertion2.Signature.Verify(theoryData.SigningCredentials.Key, theoryData.CryptoProviderFactory);
                IdentityComparer.AreEqual(samlAssertion, samlAssertion2, context);
            }
            catch (Exception ex)
            {
                theoryData.ExpectedException.ProcessException(ex, context);
            }

            TestUtilities.AssertFailIfErrors(context);
        }

        public static TheoryData<EnvelopedSignatureTheoryData> RoundTripSaml2TheoryData
        {
            get
            {
                return new TheoryData<EnvelopedSignatureTheoryData>()
                {
                    new EnvelopedSignatureTheoryData
                    {
                        ReferenceId = Default.ReferenceUriWithPrefix,
                        SigningCredentials = Default.AsymmetricSigningCredentials,
                        TestId = nameof(ReferenceTokens.Saml2Token_Valid2) + "1",
                        Xml =  ReferenceTokens.Saml2Token_Valid2
                    },
                    new EnvelopedSignatureTheoryData
                    {
                        ReferenceId = Default.ReferenceUriWithOutPrefix,
                        SigningCredentials = Default.AsymmetricSigningCredentials,
                        TestId = nameof(ReferenceTokens.Saml2Token_Valid2) + "2",
                        Xml =  ReferenceTokens.Saml2Token_Valid2
                    }
                };
            }
        }

        [Theory, MemberData(nameof(RoundTripWsMetadataTheoryData))]
        public void RoundTripWsMetadata(EnvelopedSignatureTheoryData theoryData)
        {
            var context = TestUtilities.WriteHeader($"{this}.RoundTripWsMetadata", theoryData);

            try
            {
                var settings = new XmlWriterSettings
                {
                    Encoding = new UTF8Encoding(false)
                };
                var buffer = new MemoryStream();
                var esw = new EnvelopedSignatureWriter(XmlWriter.Create(buffer, settings), theoryData.SigningCredentials, theoryData.ReferenceId);

                foreach (var action in theoryData.Action.GetInvocationList())
                {
                    action.DynamicInvoke(esw);
                }

                var metadata = Encoding.UTF8.GetString(buffer.ToArray());
                var configuration = new WsFederationConfiguration();
                var reader = XmlReader.Create(new StringReader(metadata));
                configuration = new WsFederationMetadataSerializer().ReadMetadata(reader);
                configuration.Signature.Verify(theoryData.SigningCredentials.Key, theoryData.SigningCredentials.Key.CryptoProviderFactory);
                theoryData.ExpectedException.ProcessNoException(context);
            }
            catch (Exception ex)
            {
                theoryData.ExpectedException.ProcessException(ex, context);
            }

            TestUtilities.AssertFailIfErrors(context);
        }

        public static TheoryData<EnvelopedSignatureTheoryData> RoundTripWsMetadataTheoryData
        {
            get
            {
                return new TheoryData<EnvelopedSignatureTheoryData>()
                {
                    new EnvelopedSignatureTheoryData
                    {
                        First = true,
                        Action = (EnvelopedSignatureWriter esw) =>
                        {
                            esw.WriteStartElement("EntityDescriptor", "urn:oasis:names:tc:SAML:2.0:metadata");
                            esw.WriteAttributeString("entityID", "issuer");
                            esw.WriteEndElement();
                        },
                        ReferenceId = Default.ReferenceUriWithOutPrefix,
                        SigningCredentials = Default.AsymmetricSigningCredentials,
                        TestId = "WriteSignatureAsLastElement",
                    },
                    new EnvelopedSignatureTheoryData
                    {
                        Action = (EnvelopedSignatureWriter esw) =>
                        {
                            esw.WriteStartElement("EntityDescriptor", "urn:oasis:names:tc:SAML:2.0:metadata");
                            esw.WriteAttributeString("entityID", "issuer");
                            esw.WriteStartElement("RoleDescriptor", "urn:oasis:names:tc:SAML:2.0:metadata");
                            esw.WriteEndElement();
                            esw.WriteEndElement();
                        },
                        ReferenceId = Default.ReferenceUriWithOutPrefix,
                        SigningCredentials = Default.AsymmetricSigningCredentials,
                        TestId = "WriteSignatureAsLastElement2",
                    },
                    new EnvelopedSignatureTheoryData
                    {
                        Action = (EnvelopedSignatureWriter esw) =>
                        {
                            esw.WriteStartElement("EntityDescriptor", "urn:oasis:names:tc:SAML:2.0:metadata");
                            esw.WriteAttributeString("entityID", "issuer");
                            esw.WriteSignature();
                            esw.WriteStartElement("RoleDescriptor", "urn:oasis:names:tc:SAML:2.0:metadata");
                            esw.WriteEndElement();
                            esw.WriteEndElement();
                        },
                        ReferenceId = Default.ReferenceUriWithOutPrefix,
                        SigningCredentials = Default.AsymmetricSigningCredentials,
                        TestId = "WriteSignatureInTheMiddle",
                    },
                    new EnvelopedSignatureTheoryData
                    {
                        Action = (EnvelopedSignatureWriter esw) =>
                        {
                            esw.WriteStartElement("EntityDescriptor", "urn:oasis:names:tc:SAML:2.0:metadata");
                            esw.WriteAttributeString("entityID", "issuer");
                            esw.WriteStartElement("RoleDescriptor", "urn:oasis:names:tc:SAML:2.0:metadata");
                            esw.WriteSignature();
                            esw.WriteEndElement();
                            esw.WriteEndElement();
                        },
                        ReferenceId = Default.ReferenceUriWithOutPrefix,
                        SigningCredentials = Default.AsymmetricSigningCredentials,
                        TestId = "WriteSignatureInTheMiddle2",
                    },
                    new EnvelopedSignatureTheoryData
                    {
                        Action = (EnvelopedSignatureWriter esw) =>
                        {
                            esw.WriteStartElement("EntityDescriptor", "urn:oasis:names:tc:SAML:2.0:metadata");
                            esw.WriteAttributeString("entityID", "issuer");
                            esw.WriteStartElement("RoleDescriptor", "urn:oasis:names:tc:SAML:2.0:metadata");
                            esw.WriteStartElement("KeyDescriptor", "urn:oasis:names:tc:SAML:2.0:metadata");
                            esw.WriteSignature();
                            esw.WriteEndElement();
                            esw.WriteEndElement();
                            esw.WriteEndElement();
                        },
                        ReferenceId = Default.ReferenceUriWithOutPrefix,
                        SigningCredentials = Default.AsymmetricSigningCredentials,
                        TestId = "WriteSignatureInTheMiddle3",
                    },
                    new EnvelopedSignatureTheoryData
                    {
                        Action = (EnvelopedSignatureWriter esw) =>
                        {
                            esw.WriteStartElement("EntityDescriptor", "urn:oasis:names:tc:SAML:2.0:metadata");
                            esw.WriteAttributeString("entityID", "issuer");
                            esw.WriteStartElement("RoleDescriptor", "urn:oasis:names:tc:SAML:2.0:metadata");
                            esw.WriteEndElement();
                            esw.WriteSignature();
                            esw.WriteEndElement();
                        },
                        ReferenceId = Default.ReferenceUriWithOutPrefix,
                        SigningCredentials = Default.AsymmetricSigningCredentials,
                        TestId = "WriteSignatureAsLastElementExplicitly",
                    },
                    new EnvelopedSignatureTheoryData
                    {
                        Action = (EnvelopedSignatureWriter esw) =>
                        {
                            esw.WriteStartElement("EntityDescriptor", "urn:oasis:names:tc:SAML:2.0:metadata");
                            esw.WriteAttributeString("entityID", "issuer");
                            esw.WriteStartElement("RoleDescriptor", "urn:oasis:names:tc:SAML:2.0:metadata");
                            esw.WriteEndElement();
                            esw.WriteEndElement();
                            esw.WriteSignature();
                        },
                        ReferenceId = Default.ReferenceUriWithOutPrefix,
                        SigningCredentials = Default.AsymmetricSigningCredentials,
                        TestId = "WriteSignatureAfterLastElementIsIgnored",
                    },
                    new EnvelopedSignatureTheoryData
                    {
                        Action = (EnvelopedSignatureWriter esw) =>
                        {
                            esw.WriteStartElement("EntityDescriptor", "urn:oasis:names:tc:SAML:2.0:metadata");
                            esw.WriteSignature();
                            esw.WriteAttributeString("entityID", "issuer");
                            esw.WriteStartElement("RoleDescriptor", "urn:oasis:names:tc:SAML:2.0:metadata");
                            esw.WriteEndElement();
                            esw.WriteEndElement();
                        },
                        ReferenceId = Default.ReferenceUriWithOutPrefix,
                        SigningCredentials = Default.AsymmetricSigningCredentials,
                        ExpectedException = new ExpectedException(typeof(System.Reflection.TargetInvocationException), null, typeof(System.InvalidOperationException)),
                        TestId = "WriteSignatureInTheMiddleBeforeAttributeInvalid",
                    }
                };
            }
        }

        [Fact]
        public void RoundTripSamlP()
        {
            var context = new CompareContext($"{this}.RoundTripSamlP");
            ExpectedException expectedException = ExpectedException.NoExceptionExpected;
            var samlpTokenKey = KeyingMaterial.RsaSigningCreds_4096_Public.Key;
            var samlpTokenSigningCredentials = KeyingMaterial.RsaSigningCreds_4096;
            var samlpKey = KeyingMaterial.RsaSigningCreds_2048_Public.Key;
            var samlpSigningCredentials = KeyingMaterial.RsaSigningCreds_2048;

            try
            {
                // write samlp
                var settings = new XmlWriterSettings
                {
                    Encoding = new UTF8Encoding(false)
                };
                var buffer = new MemoryStream();
                var esw = new EnvelopedSignatureWriter(XmlWriter.Create(buffer, settings), samlpSigningCredentials, "id-uAOhNLe7abGB6WGPk");
                esw.WriteStartElement("ns0", "Response", "urn:oasis:names:tc:SAML:2.0:protocol");

                esw.WriteAttributeString("ns1", "urn:oasis:names:tc:SAML:2.0:assertion");
                esw.WriteAttributeString("ns2", "http://www.w3.org/2000/09/xmldsig#");
                esw.WriteAttributeString("Destination", "https://tnia.eidentita.cz/fpsts/processRequest.aspx");
                esw.WriteAttributeString("ID", "id-uAOhNLe7abGB6WGPk");
                esw.WriteAttributeString("InResponseTo", "ida5714d006fcc430c92aacf34ab30b166");
                esw.WriteAttributeString("IssueInstant", "2019-04-08T10:30:49Z");
                esw.WriteAttributeString("Version", "2.0");
                esw.WriteSignature();
                esw.WriteStartElement("ns1", "Issuer");
                esw.WriteAttributeString("Format", "urn:oasis:names:tc:SAML:2.0:nameid-format:entity");
                esw.WriteString("https://mojeid.regtest.nic.cz/saml/idp.xml");
                esw.WriteEndElement();
                esw.WriteStartElement("ns0", "Status", null);
                esw.WriteStartElement("ns0", "StatusCode", null);
                esw.WriteAttributeString("Value", "urn:oasis:names:tc:SAML:2.0:status:Success");
                esw.WriteEndElement();
                esw.WriteEndElement();
                Saml2Serializer samlSerializer = new Saml2Serializer();
                Saml2Assertion assertion = CreateAssertion(samlpTokenSigningCredentials);
                samlSerializer.WriteAssertion(esw, assertion);
                esw.WriteEndElement();
                var xml = Encoding.UTF8.GetString(buffer.ToArray());

                // read samlp and verify signatures
                XmlReader reader = XmlUtilities.CreateDictionaryReader(xml);
                IXmlElementReader tokenReaders = new TokenReaders(new List<SecurityTokenHandler> { new Saml2SecurityTokenHandler() });
                EnvelopedSignatureReader envelopedReader = new EnvelopedSignatureReader(reader, tokenReaders);

                while (envelopedReader.Read()) ;

                foreach (var item in tokenReaders.Items)
                {
                    if (item is Saml2SecurityToken samlToken)
                        samlToken.Assertion.Signature.Verify(samlpTokenKey);
                }

                envelopedReader.Signature.Verify(samlpKey, samlpKey.CryptoProviderFactory);
                expectedException.ProcessNoException(context);
            }
            catch (Exception ex)
            {
                expectedException.ProcessException(ex, context);
            }

            TestUtilities.AssertFailIfErrors(context);
        }

        [Fact]
        public void RoundTripSamlPSignatureAfterAssertion()
        {
            var context = new CompareContext($"{this}.RoundTripSamlPSignatureAfterAssertion");
            ExpectedException expectedException = ExpectedException.NoExceptionExpected;
            var samlpTokenKey = KeyingMaterial.RsaSigningCreds_4096_Public.Key;
            var samlpTokenSigningCredentials = KeyingMaterial.RsaSigningCreds_4096;
            var samlpKey = KeyingMaterial.RsaSigningCreds_2048_Public.Key;
            var samlpSigningCredentials = KeyingMaterial.RsaSigningCreds_2048;

            try
            {
                // write samlp
                var settings = new XmlWriterSettings
                {
                    Encoding = new UTF8Encoding(false)
                };
                var buffer = new MemoryStream();
                var esw = new EnvelopedSignatureWriter(XmlWriter.Create(buffer, settings), samlpSigningCredentials, "id-uAOhNLe7abGB6WGPk");
                esw.WriteStartElement("ns0", "Response", "urn:oasis:names:tc:SAML:2.0:protocol");

                esw.WriteAttributeString("ns1", "urn:oasis:names:tc:SAML:2.0:assertion");
                esw.WriteAttributeString("ns2", "http://www.w3.org/2000/09/xmldsig#");
                esw.WriteAttributeString("Destination", "https://tnia.eidentita.cz/fpsts/processRequest.aspx");
                esw.WriteAttributeString("ID", "id-uAOhNLe7abGB6WGPk");
                esw.WriteAttributeString("InResponseTo", "ida5714d006fcc430c92aacf34ab30b166");
                esw.WriteAttributeString("IssueInstant", "2019-04-08T10:30:49Z");
                esw.WriteAttributeString("Version", "2.0");
                esw.WriteStartElement("ns1", "Issuer");
                esw.WriteAttributeString("Format", "urn:oasis:names:tc:SAML:2.0:nameid-format:entity");
                esw.WriteString("https://mojeid.regtest.nic.cz/saml/idp.xml");
                esw.WriteEndElement();
                esw.WriteStartElement("ns0", "Status", null);
                esw.WriteStartElement("ns0", "StatusCode", null);
                esw.WriteAttributeString("Value", "urn:oasis:names:tc:SAML:2.0:status:Success");
                esw.WriteEndElement();
                esw.WriteEndElement();
                Saml2Serializer samlSerializer = new Saml2Serializer();
                Saml2Assertion assertion = CreateAssertion(samlpTokenSigningCredentials);
                samlSerializer.WriteAssertion(esw, assertion);
                esw.WriteSignature();
                esw.WriteEndElement();
                var xml = Encoding.UTF8.GetString(buffer.ToArray());

                // read samlp and verify signatures
                XmlReader reader = XmlUtilities.CreateDictionaryReader(xml);
                IXmlElementReader tokenReaders = new TokenReaders(new List<SecurityTokenHandler> { new Saml2SecurityTokenHandler() });
                EnvelopedSignatureReader envelopedReader = new EnvelopedSignatureReader(reader, tokenReaders);

                while (envelopedReader.Read()) ;

                foreach (var item in tokenReaders.Items)
                {
                    if (item is Saml2SecurityToken samlToken)
                        samlToken.Assertion.Signature.Verify(samlpTokenKey);
                }

                envelopedReader.Signature.Verify(samlpKey, samlpKey.CryptoProviderFactory);
                expectedException.ProcessNoException(context);
            }
            catch (Exception ex)
            {
                expectedException.ProcessException(ex, context);
            }

            TestUtilities.AssertFailIfErrors(context);
        }

        private static Saml2Assertion CreateAssertion(SigningCredentials samlpTokenSigningCredentials)
        {
            var assertion = new Saml2Assertion(new Saml2NameIdentifier("https://mojeid.regtest.nic.cz/saml/idp.xml", new Uri("urn:oasis:names:tc:SAML:2.0:nameid-format:entity")))
            {
                InclusiveNamespacesPrefixList = "ns1 ns2",
                IssueInstant = DateTime.Parse("2019-04-08T10:30:49Z"),
                SigningCredentials = samlpTokenSigningCredentials,
                Id = new Saml2Id("id-2bMsOPOKIqeVIDLqJ")
            };

            var saml2SubjectConfirmationData = new Saml2SubjectConfirmationData
            {
                InResponseTo = new Saml2Id("ida5714d006fcc430c92aacf34ab30b166"),
                NotOnOrAfter = DateTime.Parse("2019-04-08T10:45:49Z"),
                Recipient = new Uri("https://tnia.eidentita.cz/fpsts/processRequest.aspx")
            };
            var saml2SubjectConfirmation = new Saml2SubjectConfirmation(new Uri("urn:oasis:names:tc:SAML:2.0:cm:bearer"), saml2SubjectConfirmationData);
            var saml2Subject = new Saml2Subject(new Saml2NameIdentifier("6dfe0399103d11411b1fa00772b6a13e0858605b80c20ea845769c57b41479ed", new Uri("urn:oasis:names:tc:SAML:2.0:nameid-format:persistent")));
            saml2Subject.SubjectConfirmations.Add(saml2SubjectConfirmation);
            assertion.Subject = saml2Subject;

            var saml2AudienceRestrictions = new Saml2AudienceRestriction("urn:microsoft: cgg2010: fpsts");
            var saml2Conditions = new Saml2Conditions();
            saml2Conditions.AudienceRestrictions.Add(saml2AudienceRestrictions);
            saml2Conditions.NotBefore = DateTime.Parse("2019-04-08T10:30:49Z");
            saml2Conditions.NotOnOrAfter = DateTime.Parse("2019-04-08T10:45:49Z");
            assertion.Conditions = saml2Conditions;

            var saml2AuthenticationContext = new Saml2AuthenticationContext(new Uri("urn:oasis:names:tc:SAML:2.0:ac:classes:PasswordProtectedTransport"), null);
            var saml2Statement = new Saml2AuthenticationStatement(saml2AuthenticationContext)
            {
                AuthenticationInstant = DateTime.Parse("2019-04-08T10:30:49Z"),
                SessionIndex = "id-oTnhrqWtcTTEntvMy"
            };
            assertion.Statements.Add(saml2Statement);

            return assertion;
        }
    }
}

#pragma warning restore CS3016 // Arrays as attribute arguments is not CLS-compliant
