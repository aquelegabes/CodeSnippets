// requires itextsharp

using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.security;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Namespace.Here
{
    public static class CertificateMethods
    {
        /// <summary>
        /// Lista todos os certificados disponíveis na conta de usuário.
        /// </summary>
        /// <returns></returns>
        public static List<Certificado> ListarCertificadosDisponiveis()
        {
            var result = new List<Certificado>();
            Certificado certModel;
            X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            try
            {
                //Criar um armazenamento de certificado utilizando a conta de usuário.
                if (store == null)
                    throw new Exception("Não há certificado digital disponível!");

                //Abrindo o armazenamento de certificados.
                store.Open(OpenFlags.ReadOnly);

                foreach (X509Certificate2 cert in store.Certificates)
                {
                    if (cert.Subject.Contains("IMPRENSA") ||
                        cert.Subject.Contains("e-CPF") ||
                        cert.Subject.Contains("ICP") )
                    {
                        certModel = new Certificado(cert.Subject, cert.Issuer);
                        certModel.ValidadeInicio = Convert.ToDateTime(cert.GetEffectiveDateString());
                        certModel.ValidadeFim = Convert.ToDateTime(cert.GetExpirationDateString());
                        result.Add(certModel);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Erro durante a pesquisa de certificado: {ex.Message}");
            }
            finally
            {
                store.Close();
            }
        }

        /// <summary>
        /// Seleciona um certificado através de um nome comum
        /// </summary>
        /// <param name="nomeComum">Pode ser usado o CPF</param>
        /// <returns></returns>
        public static UserAssinador SelecionarCertificado(string nomeComum)
        {
            if (string.IsNullOrWhiteSpace(nomeComum))
                return null;

            X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            try
            {
                store.Open(OpenFlags.ReadOnly);

                foreach(X509Certificate2 cert in store.Certificates)
                {
                    if (cert.Subject.Contains(nomeComum))
                    {
                        try
                        {
                            if (cert.PrivateKey != null)
                                return new UserAssinador { CertificadoUsuario = cert };
                        }
                        catch (Exception ex)
                        {
                            return null;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return null;
            }
            finally
            {
                store.Close();
            }
            return null;
        }

        /// <summary>
        /// Assina um pdf com um certificado válido
        /// </summary>
        /// <param name="certificate">X509 Certificado</param>
        /// <param name="dadosAssinatura">Dados da assinatura (DadosAssinatura)</param>
        /// <returns></returns>
        public static byte[] AssinarPdf(X509Certificate2 certificate, DadosAssinatura dadosAssinatura)
        {
            try
            {
                // ler arquivo e insere dados de assinatura
                using (PdfReader reader = new PdfReader(dadosAssinatura.Arquivo))
                {
                    using (MemoryStream fout = new MemoryStream())
                    {
                        PdfStamper stamper = PdfStamper.CreateSignature(reader, fout, '\0', null, true);

                        // texto marca d'água
                        Font f = new Font(Font.FontFamily.TIMES_ROMAN, 8);

                        string[] dados = certificate.GetNameInfo(X509NameType.SimpleName, false).Split(':');
                        string codAut = $"{DateTime.Now.ToString("yyMMddssff")}-{dados[1].Substring(dados[1].Length - 4, 4)}";

                        Phrase pAssinado = new Phrase($@"Este documento foi assinado digitalmente por: {dados[0]}, para validar este documento acesse http://vre.jucesp.sp.gov.br e informe", f);
                        Phrase pCodigAut = new Phrase($@" o código de autenticidade: Nº{codAut} - {DateTime.Now.ToLongDateString()}", f);
                        // Imagem marca d'água
                        //Image img = dadosAssinatura.Imagem;
                        float w = 200F;
                        float h = 75.2F;
                        // Transparência
                        PdfGState gs1 = new PdfGState();

                        // Propriedades
                        PdfContentByte over;
                        Rectangle pagesize;

                        int n = reader.NumberOfPages;

                        //Página
                        var pagina = 1;
                        bool todasPaginas = false;
                        pagesize = reader.GetPageSizeWithRotation(pagina);

                        switch (dadosAssinatura.PaginaAssinatura)
                        {
                            case EnumPaginaAssinatura.PRIMEIRA:
                                pagina = 1;
                                break;
                            case EnumPaginaAssinatura.ULTIMA:
                                pagina = reader.NumberOfPages;
                                break;
                            case EnumPaginaAssinatura.TODAS:
                                todasPaginas = true;
                                break;
                            default:
                                pagina = 1;
                                break;
                        }

                        float x, y, xr = 0, hr = 0, yr = 0, wr = 0;
                        //Posição da assinatura
                        switch (dadosAssinatura.Posicao)
                        {
                            case EnumPosicao.ACIMA_ESQUERDA:
                                x = (float)(pagesize.Left * 0.88);
                                y = (float)(pagesize.Top * 0.88);
                                xr = x * 0.5F;
                                wr = w;
                                yr = pagesize.Top * 0.97F;
                                hr = pagesize.Top * 0.88F;

                                break;
                            case EnumPosicao.ACIMA_DIREITA:
                                x = (float)(pagesize.Right * 0.64);
                                y = (float)(pagesize.Top * 0.88);
                                xr = pagesize.Right * 0.97F;
                                wr = xr - w;
                                yr = pagesize.Top * 0.97F;
                                hr = pagesize.Top * 0.88F;
                                break;
                            case EnumPosicao.ABAIXO_ESQUERDA:
                                x = (float)(pagesize.Left * 0.88);
                                y = (float)(pagesize.Bottom * 0.88);
                                xr = x * 0.5F;
                                wr = w;
                                yr = y;
                                hr = h;
                                break;
                            case EnumPosicao.ABAIXO_DIREITA:
                                x = (float)(pagesize.Right * 0.64);
                                y = (float)(pagesize.Bottom * 0.88);
                                xr = x * 1.53F;
                                wr = w * 1.9F;
                                yr = y;
                                hr = h;
                                break;
                            case EnumPosicao.ABAIXO_CENTRO:
                                x = (pagesize.Right * 1.74f - pagesize.Left) / 1.32f;
                                y = (float)(pagesize.Bottom * 0.76);
                                xr = x * 1.53F;
                                wr = w * 1.53F;
                                yr = y;
                                hr = h;
                                break;
                            default:
                                x = (float)(pagesize.Left * 0.88);
                                y = (float)(pagesize.Top * 0.88);
                                xr = x * 1.53F;
                                wr = w * 1.9F;
                                break;
                        }

                        PdfSignatureAppearance appearance = stamper.SignatureAppearance;
                        appearance.SignatureRenderingMode = PdfSignatureAppearance.RenderingMode.DESCRIPTION;
                        appearance.Layer2Text = "";
                        appearance.Layer4Text = "";
                        Rectangle rect = new Rectangle(wr, hr, xr, yr);

                        //Plota a assinatura no pdf
                        if (todasPaginas)
                        {
                            for (int i = 1; i <= n; i++) {
                                over = stamper.GetOverContent(i);
                                over.SaveState();
                                over.SetGState(gs1);
                                //over.AddImage(img, w, 0, 0, h, x, y);
                                ColumnText.ShowTextAligned(over, Element.ALIGN_TOP, pAssinado, x + 57, y + 15, 0);
                                ColumnText.ShowTextAligned(over, Element.ALIGN_TOP, pCodigAut, x + 84.5f, y + 5, 0);
                                over.RestoreState();
                            }
                        }
                        else
                        {
                            over = stamper.GetOverContent(pagina);

                            over.SaveState();
                            over.SetGState(gs1);
                            //over.AddImage(img, w, 0, 0, h, x, y);
                            ColumnText.ShowTextAligned(over, Element.ALIGN_TOP, pAssinado, x + 57, y + 15, 0);
                            ColumnText.ShowTextAligned(over, Element.ALIGN_TOP, pCodigAut, x + 84.5f, y + 5, 0);
                            over.RestoreState();
                        }

                        
                        
                        ICollection<Org.BouncyCastle.X509.X509Certificate> certChain;
                        IExternalSignature es = ResolveExternalSignatureFromCertStore(certificate, dadosAssinatura.CertificadoValido, out certChain);

                        //Autenticação da assinatura digital
                        MakeSignature.SignDetached(appearance, es, certChain, null, null, null, 0, CryptoStandard.CADES);

                        stamper.Close();
                        return fout.ToArray();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Erro durante assinatura digital do pdf: {ex.Message}");
            }
        }

        private static IExternalSignature ResolveExternalSignatureFromCertStore(X509Certificate2 cert, bool allowInvalidCertificate, out ICollection<Org.BouncyCastle.X509.X509Certificate> chain)
        {
            try
            {
                X509Certificate2 signatureCert = new X509Certificate2(cert);
                Org.BouncyCastle.X509.X509Certificate bcCert = Org.BouncyCastle.Security.DotNetUtilities.FromX509Certificate(cert);
                chain = new List<Org.BouncyCastle.X509.X509Certificate> { bcCert };

                var parser = new Org.BouncyCastle.X509.X509CertificateParser();
                var bouncyCertificate = parser.ReadCertificate(cert.GetRawCertData());
                var algorithm = DigestAlgorithms.GetDigest(bouncyCertificate.SigAlgOid);
                return new X509Certificate2Signature(signatureCert, algorithm);
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
