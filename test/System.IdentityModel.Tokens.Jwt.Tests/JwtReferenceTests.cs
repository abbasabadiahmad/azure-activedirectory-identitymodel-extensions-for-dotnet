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

using System.Text;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace System.IdentityModel.Tokens.Jwt.Tests
{
    /// <summary>
    /// 
    /// </summary>
    public class JwtReferenceTests
    {
#pragma warning disable CS3016 // Arrays as attribute arguments is not CLS-compliant
        [Theory, MemberData("Base64UrlEncodingTheoryData")]
#pragma warning restore CS3016 // Arrays as attribute arguments is not CLS-compliant

        public void Base64UrlEncoding(string testId, string dataToEncode, string encodedData)
        {
            Assert.True(dataToEncode.Equals(Base64UrlEncoder.Decode(encodedData), StringComparison.Ordinal), "dataToEncode.Equals(Base64UrlEncoder.Decode(encodedData), StringComparison.Ordinal)");
            Assert.True(encodedData.Equals(Base64UrlEncoder.Encode(dataToEncode), StringComparison.Ordinal), "encodedData.Equals(Base64UrlEncoder.Encode(dataToEncode), StringComparison.Ordinal)");
        }

        public static TheoryData<string, string, string> Base64UrlEncodingTheoryData
        {
            get
            {
                var theoryData = new TheoryData<string, string, string>();

                theoryData.Add("Test1", RFC7520References.Payload, RFC7520References.PayloadEncoded);
                theoryData.Add("Test2", RFC7520References.RSAHeaderJson, RFC7520References.RSAHeaderEncoded);
                theoryData.Add("Test3", RFC7520References.ES512HeaderJson, RFC7520References.ES512HeaderEncoded);
                theoryData.Add("Test4", RFC7520References.SymmetricHeaderJson, RFC7520References.SymmetricHeaderEncoded);

                return theoryData;
            }
        }

#pragma warning disable CS3016 // Arrays as attribute arguments is not CLS-compliant
        [Theory, MemberData("JwtEncodingTheoryData")]
#pragma warning restore CS3016 // Arrays as attribute arguments is not CLS-compliant
        public void JwtEncoding(string testId, JwtHeader header, string encodedData)
        {
            Assert.True(encodedData.Equals(header.Base64UrlEncode(), StringComparison.Ordinal), "encodedData.Equals(header.Base64UrlEncode(), StringComparison.Ordinal)");
        }

        public static TheoryData<string, JwtHeader, string> JwtEncodingTheoryData
        {
            get
            {
                var theoryData = new TheoryData<string, JwtHeader, string>();

                theoryData.Add("Test1", RFC7520References.ES512JwtHeader, RFC7520References.ES512HeaderEncoded);
                theoryData.Add("Test2", RFC7520References.RSAJwtHeader, RFC7520References.RSAHeaderEncoded);
                theoryData.Add("Test3", RFC7520References.SymmetricJwtHeader, RFC7520References.SymmetricHeaderEncoded);

                return theoryData;
            }
        }

#pragma warning disable CS3016 // Arrays as attribute arguments is not CLS-compliant
        [Theory, MemberData("JwtSigningTheoryData")]
#pragma warning restore CS3016 // Arrays as attribute arguments is not CLS-compliant
        public void JwtSigning(JwtSigningTestParams testParams)
        {
            var providerForSigning = CryptoProviderFactory.Default.CreateForSigning(testParams.PrivateKey, testParams.Algorithm);
            var providerForVerifying = CryptoProviderFactory.Default.CreateForVerifying(testParams.PublicKey, testParams.Algorithm);
            var signatureBytes = providerForSigning.Sign(Encoding.UTF8.GetBytes(testParams.EncodedData));
            var encodedSignature = Base64UrlEncoder.Encode(signatureBytes);

            Assert.True(testParams.EncodedSignature.Equals(encodedSignature, StringComparison.Ordinal), "encodedSignature != testParams.EncodedSignature");
            Assert.True(providerForVerifying.Verify(Encoding.UTF8.GetBytes(testParams.EncodedData), Base64UrlEncoder.DecodeBytes(testParams.EncodedSignature)), "Verify Failed");
        }

        public static TheoryData<JwtSigningTestParams> JwtSigningTheoryData
        {
            get
            {
                var theoryData = new TheoryData<JwtSigningTestParams>();

                theoryData.Add(new JwtSigningTestParams
                {
                    Algorithm = RFC7520References.RSAJwtHeader.Alg,
                    EncodedData = RFC7520References.RSAEncoded,
                    EncodedSignature = RFC7520References.RSASignatureEncoded,
                    PrivateKey = RFC7520References.RSASigningPrivateKey,
                    PublicKey = RFC7520References.RSASigningPublicKey,
                    TestId = "Test1"
                });

                // clr runtime is failing to create the key.
                //theoryData.Add(new JwtSigningTestParams
                //{
                //    Algorithm = RFC7520References.ES512JwtHeader.Alg,
                //    EncodedData = RFC7520References.ES512Encoded,
                //    EncodedSignature = RFC7520References.ES512SignatureEncoded,
                //    PrivateKey = RFC7520References.ECDsaPrivateKey,
                //    PublicKey = RFC7520References.ECDsaPublicKey,
                //    TestId = "Test2"
                //});

                theoryData.Add(new JwtSigningTestParams
                {
                    Algorithm = RFC7520References.SymmetricJwtHeader.Alg,
                    EncodedData = RFC7520References.SymmetricEncoded,
                    EncodedSignature = RFC7520References.SymmetricSignatureEncoded,
                    PrivateKey = RFC7520References.SymmetricKeyMac,
                    PublicKey = RFC7520References.SymmetricKeyMac,
                    TestId = "Test3"
                });

                return theoryData;
            }
        }

        public class JwtSigningTestParams
        {
            public string Algorithm { get; set; }
            public string EncodedData { get; set; }
            public string EncodedSignature { get; set; }
            public SecurityKey PrivateKey { get; set; }
            public SecurityKey PublicKey { get; set; }
            public string TestId { get; set; }

            public override string ToString()
            {
                return TestId + ", " + PrivateKey.KeyId + ", " + PublicKey.KeyId;
            }
        }
    }
}