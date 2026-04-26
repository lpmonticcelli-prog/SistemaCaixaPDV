using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Drawing;           // O novo motor gráfico
using System.Drawing.Printing;  // O salvador da Bobina Infinita
using System.Net;
using System.Windows;           // Apenas para as caixas de mensagem

namespace SistemaCaixaPDV
{
    public static class ImpressorCupom
    {
        public static void Imprimir(List<ItemVenda> itens, decimal subtotal, decimal desconto, decimal totalLiquido, string formaPagamento, string nomeCliente, string tipoImpressora)
        {
            var config = BancoDeDados.ObterConfiguracoes();
            if (string.IsNullOrEmpty(tipoImpressora)) tipoImpressora = config.TipoImpressora;

            int colunas = 40;
            if (tipoImpressora == "58mm") colunas = 26;
            if (tipoImpressora == "A4") colunas = 80;

            string pastaXml = @"C:\SistemaPDV\XML_Gerados\NFCe";
            string chaveSefaz = "";
            string protocoloSefaz = "";
            string qrCodeSefaz = "";

            try
            {
                if (Directory.Exists(pastaXml))
                {
                    var dir = new DirectoryInfo(pastaXml);
                    var ultimoXml = dir.GetFiles("*-nfce.xml").OrderByDescending(f => f.CreationTime).FirstOrDefault();

                    if (ultimoXml != null && (DateTime.Now - ultimoXml.CreationTime).TotalSeconds < 15)
                    {
                        string xml = File.ReadAllText(ultimoXml.FullName);
                        chaveSefaz = ultimoXml.Name.Replace("-nfce.xml", "");

                        var mProt = Regex.Match(xml, @"<nProt>(.*?)</nProt>");
                        var mQr = Regex.Match(xml, @"<qrCode>.*?\[CDATA\[(.*?)\].*?</qrCode>");
                        if (!mQr.Success) mQr = Regex.Match(xml, @"<qrCode>(.*?)</qrCode>");

                        if (mProt.Success) protocoloSefaz = mProt.Groups[1].Value;
                        if (mQr.Success) qrCodeSefaz = mQr.Groups[1].Value.Replace("&amp;", "&");
                    }
                }
            }
            catch { }

            StringBuilder sb = new StringBuilder();

            sb.AppendLine(Centralizar(string.IsNullOrEmpty(config.NomeLoja) ? "MINHA LOJA" : config.NomeLoja.ToUpper(), colunas));
            if (!string.IsNullOrEmpty(config.Cnpj)) sb.AppendLine(Centralizar("CNPJ: " + config.Cnpj, colunas));
            if (!string.IsNullOrEmpty(config.Telefone)) sb.AppendLine(Centralizar("Tel: " + config.Telefone, colunas));
            if (!string.IsNullOrEmpty(config.EnderecoCompleto)) sb.AppendLine(Centralizar(config.EnderecoCompleto, colunas));

            sb.AppendLine(GerarLinha('=', colunas));

            if (!string.IsNullOrEmpty(chaveSefaz))
            {
                sb.AppendLine(Centralizar("Documento Auxiliar da Nota", colunas));
                sb.AppendLine(Centralizar("Fiscal de Consumidor Eletronica", colunas));
            }
            else
            {
                sb.AppendLine(Centralizar("CUPOM NAO FISCAL", colunas));
            }

            sb.AppendLine(GerarLinha('=', colunas));

            string dataStr = $"DT:{DateTime.Now.ToString("dd/MM/yy")}";
            string horaStr = $"HR:{DateTime.Now.ToString("HH:mm")}";
            sb.AppendLine(GerarExtremos(dataStr, horaStr, colunas));

            string clienteStr = string.IsNullOrWhiteSpace(nomeCliente) ? "CONSUMIDOR FINAL" : nomeCliente.ToUpper();
            string linhaCli = $"CLI: {clienteStr}";
            if (linhaCli.Length > colunas) linhaCli = linhaCli.Substring(0, colunas);
            sb.AppendLine(linhaCli);

            string pagStr = $"PAG: {formaPagamento.ToUpper()}";
            if (pagStr.Length > colunas) pagStr = pagStr.Substring(0, colunas);
            sb.AppendLine(pagStr);
            sb.AppendLine(GerarLinha('-', colunas));

            if (colunas == 26) sb.AppendLine(GerarExtremos("QTDxUN", "TOTAL", colunas));
            else sb.AppendLine(string.Format("{0,-4}{1,-17} {2,8} {3,9}", "QTD", "DESCRICAO", "VL.UN", "TOTAL"));

            sb.AppendLine(GerarLinha('-', colunas));

            foreach (var item in itens)
            {
                if (colunas == 26)
                {
                    string desc = item.Descricao.Length > colunas ? item.Descricao.Substring(0, colunas) : item.Descricao;
                    sb.AppendLine(desc);

                    string ladoEsquerdo = $"{item.Quantidade}x {item.ValorUnitario:N2}";
                    string ladoDireito = item.Subtotal.ToString("N2");
                    sb.AppendLine(GerarExtremos(ladoEsquerdo, ladoDireito, colunas));
                }
                else
                {
                    string qtd = $"{item.Quantidade}x".PadRight(4);
                    string desc = item.Descricao.Length > 17 ? item.Descricao.Substring(0, 17) : item.Descricao.PadRight(17);
                    string un = item.ValorUnitario.ToString("N2").PadLeft(8);
                    string tot = item.Subtotal.ToString("N2").PadLeft(9);
                    sb.AppendLine($"{qtd}{desc} {un} {tot}");
                }
            }

            sb.AppendLine(GerarLinha('-', colunas));

            sb.AppendLine(GerarExtremos("SUBTOTAL:", $"{subtotal:N2}", colunas));
            if (desconto > 0) sb.AppendLine(GerarExtremos("DESC:", $"-{desconto:N2}", colunas));

            sb.AppendLine(GerarLinha(' ', colunas));
            sb.AppendLine(GerarExtremos("TOTAL:", $"R$ {totalLiquido:N2}", colunas));

            if (!string.IsNullOrEmpty(chaveSefaz))
            {
                sb.AppendLine(GerarLinha(' ', colunas));
                sb.AppendLine(GerarLinha('=', colunas));
                sb.AppendLine(Centralizar("AREA FISCAL - NFC-e", colunas));
                sb.AppendLine(GerarLinha('=', colunas));

                sb.AppendLine(Centralizar("Consulte a Chave em:", colunas));
                sb.AppendLine(Centralizar("www.nfce.fazenda.sp.gov.br", colunas));
                sb.AppendLine(GerarLinha(' ', colunas));

                string chaveFormatada = "";
                for (int i = 0; i < chaveSefaz.Length; i += 4)
                    chaveFormatada += chaveSefaz.Substring(i, Math.Min(4, chaveSefaz.Length - i)) + " ";

                chaveFormatada = chaveFormatada.Trim();

                sb.AppendLine(Centralizar("CHAVE DE ACESSO", colunas));

                if (colunas == 26)
                {
                    sb.AppendLine(Centralizar(chaveFormatada.Substring(0, 22).Trim(), colunas));
                    sb.AppendLine(Centralizar(chaveFormatada.Substring(22, 22).Trim(), colunas));
                    if (chaveFormatada.Length > 44) sb.AppendLine(Centralizar(chaveFormatada.Substring(44).Trim(), colunas));
                }
                else
                {
                    sb.AppendLine(Centralizar(chaveFormatada.Substring(0, 29).Trim(), colunas));
                    sb.AppendLine(Centralizar(chaveFormatada.Substring(29).Trim(), colunas));
                }

                sb.AppendLine(GerarLinha(' ', colunas));
                sb.AppendLine(Centralizar("PROTOCOLO DE AUTORIZACAO", colunas));
                sb.AppendLine(Centralizar(protocoloSefaz, colunas));
                sb.AppendLine(GerarLinha('-', colunas));
            }

            if (!string.IsNullOrEmpty(config.CabecalhoCupom))
            {
                sb.AppendLine(Centralizar(config.CabecalhoCupom, colunas));
                sb.AppendLine(GerarLinha('-', colunas));
            }
            if (!string.IsNullOrEmpty(config.RodapeCupom))
            {
                sb.AppendLine(Centralizar(config.RodapeCupom, colunas));
                sb.AppendLine(GerarLinha('-', colunas));
            }
            sb.AppendLine(Centralizar("Obrigado pela preferencia!", colunas));
            sb.AppendLine(GerarLinha(' ', colunas));
            sb.AppendLine(GerarLinha(' ', colunas));

            EnviarParaImpressora(sb.ToString(), tipoImpressora, qrCodeSefaz);
        }

        private static string Centralizar(string texto, int tamanho)
        {
            if (texto.Length >= tamanho) return texto.Substring(0, tamanho);
            int espacos = (tamanho - texto.Length) / 2;
            return texto.PadLeft(texto.Length + espacos).PadRight(tamanho);
        }

        private static string GerarLinha(char c, int tamanho) => new string(c, tamanho);

        private static string GerarExtremos(string esquerda, string direita, int tamanho)
        {
            int espacos = tamanho - esquerda.Length - direita.Length;
            if (espacos < 1) espacos = 1;
            return esquerda + new string(' ', espacos) + direita;
        }

        // =========================================================================
        // O NOVO MOTOR GRÁFICO (COM BOBINA INFINITA DINÂMICA)
        // =========================================================================
        private static void EnviarParaImpressora(string cupomText, string tipoImpressora, string qrCodeUrl)
        {
            MemoryStream msQr = null;
            Image imgQrCache = null;

            try
            {
                PrintDocument pd = new PrintDocument();

                Font fonteCupom = new Font("Courier New", tipoImpressora == "58mm" ? 8 : 10, System.Drawing.FontStyle.Bold);
                int larguraDesenho = tipoImpressora == "58mm" ? 195 : 290;

                // 1. CALCULAMOS A ALTURA EXATA DO TEXTO ANTES DE IMPRIMIR
                int alturaTotal = 0;
                using (Bitmap bmp = new Bitmap(1, 1))
                {
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        SizeF tamanhoTexto = g.MeasureString(cupomText, fonteCupom, larguraDesenho);
                        alturaTotal = (int)Math.Ceiling(tamanhoTexto.Height);
                    }
                }

                // 2. BAIXAMOS O QR CODE PARA A MEMÓRIA E SOMAMOS A ALTURA DELE
                if (!string.IsNullOrEmpty(qrCodeUrl))
                {
                    try
                    {
                        string urlApi = $"https://api.qrserver.com/v1/create-qr-code/?size=250x250&margin=0&data={Uri.EscapeDataString(qrCodeUrl)}";
                        using (WebClient client = new WebClient())
                        {
                            byte[] imgBytes = client.DownloadData(urlApi);
                            msQr = new MemoryStream(imgBytes);
                            imgQrCache = Image.FromStream(msQr);

                            alturaTotal += (tipoImpressora == "58mm" ? 140 : 190);
                        }
                    }
                    catch { }
                }

                // Adiciona uma folga grande no final para a guilhotina passar por baixo do QR Code!
                alturaTotal += 80;

                // 3. A MÁGICA DA BOBINA: Cria um papel personalizado no tamanho EXATO
                // (228 = 58mm | 314 = 80mm em centésimos de polegada)
                int larguraBobina = tipoImpressora == "58mm" ? 228 : 314;
                pd.DefaultPageSettings.PaperSize = new PaperSize("BobinaCustom", larguraBobina, alturaTotal);
                pd.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);

                pd.PrintPage += (sender, args) =>
                {
                    // Pinta o texto
                    SizeF tamanhoTexto = args.Graphics.MeasureString(cupomText, fonteCupom, larguraDesenho);
                    args.Graphics.DrawString(cupomText, fonteCupom, Brushes.Black, new RectangleF(0, 0, larguraDesenho, tamanhoTexto.Height));

                    // Pinta o QR Code
                    if (imgQrCache != null)
                    {
                        float yAtual = tamanhoTexto.Height;
                        float tamanhoQr = tipoImpressora == "58mm" ? 130 : 180;
                        float eixoX = (larguraDesenho - tamanhoQr) / 2;
                        args.Graphics.DrawImage(imgQrCache, eixoX, yAtual + 5, tamanhoQr, tamanhoQr);
                    }

                    args.HasMorePages = false; // Avisa que não tem folha 2 para evitar loops
                };

                pd.Print(); // Dispara sem abrir nenhuma janela
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro de impressão: " + ex.Message, "Falha", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // Libera a imagem da memória SÓ DEPOIS que a impressora terminar de puxar
                if (imgQrCache != null) imgQrCache.Dispose();
                if (msQr != null) msQr.Dispose();
            }
        }
    }
}