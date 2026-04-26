using System;
using System.Drawing;
using System.Drawing.Printing;

namespace SistemaCaixaPDV
{
    public class ImpressorDanfeNFCe
    {
        private NFe.Classes.NFe _nfe;
        private string _logoPath;

        // Fontes super compactas para caber na área útil da 58mm
        private Font fonteTitulo = new Font("Arial", 7, FontStyle.Bold);
        private Font fonteNormal = new Font("Arial", 6.5f, FontStyle.Regular);
        private Font fontePequena = new Font("Arial", 5.5f, FontStyle.Regular);
        private Font fonteNegrito = new Font("Arial", 6.5f, FontStyle.Bold);

        public void Imprimir(NFe.Classes.NFe notaSefaz, string caminhoLogo = "")
        {
            _nfe = notaSefaz;
            _logoPath = caminhoLogo;

            PrintDocument pd = new PrintDocument();

            // Zera margens do próprio papel no Windows
            pd.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);
            pd.PrintPage += new PrintPageEventHandler(pd_PrintPage);

            try
            {
                pd.Print();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Erro ao imprimir: " + ex.Message, "Erro", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void pd_PrintPage(object sender, PrintPageEventArgs e)
        {
            Graphics g = e.Graphics;

            // ========================================================
            // A MÁGICA ACONTECE AQUI:
            // Ignoramos o Windows e cravamos o tamanho real da 58mm!
            // ========================================================
            float larguraPapel = 185; // Se ainda cortar, abaixe para 175!
            float margemEsq = 0;      // Zero absoluto, colado na parede esquerda!
            float margemDir = larguraPapel;
            float centro = larguraPapel / 2;
            float yPos = 2;           // Começa bem no topo

            StringFormat formatoCentro = new StringFormat { Alignment = StringAlignment.Center };
            StringFormat formatoDireita = new StringFormat { Alignment = StringAlignment.Far };
            StringFormat formatoEsquerda = new StringFormat { Alignment = StringAlignment.Near };

            // 1. LOGO DA EMPRESA (Bem centralizada no novo tamanho)
            if (!string.IsNullOrEmpty(_logoPath) && System.IO.File.Exists(_logoPath))
            {
                try
                {
                    Image logo = Image.FromFile(_logoPath);
                    g.DrawImage(logo, centro - 30, yPos, 60, 60);
                    yPos += 65;
                }
                catch { }
            }

            // 2. CABEÇALHO
            var emit = _nfe.infNFe.emit;
            RectangleF rectCabecalho = new RectangleF(margemEsq, yPos, larguraPapel, 40);
            g.DrawString(emit.xNome, fonteTitulo, Brushes.Black, rectCabecalho, formatoCentro);
            yPos += g.MeasureString(emit.xNome, fonteTitulo, (int)larguraPapel).Height;

            g.DrawString($"CNPJ: {emit.CNPJ}", fonteNormal, Brushes.Black, centro, yPos, formatoCentro);
            yPos += 10;
            g.DrawString($"IE: {emit.IE}", fonteNormal, Brushes.Black, centro, yPos, formatoCentro);
            yPos += 10;

            string endereco = $"{emit.enderEmit.xLgr}, {emit.enderEmit.nro} - {emit.enderEmit.xMun}/{emit.enderEmit.UF}";
            RectangleF rectEnd = new RectangleF(margemEsq, yPos, larguraPapel, 30);
            g.DrawString(endereco, fontePequena, Brushes.Black, rectEnd, formatoCentro);
            yPos += g.MeasureString(endereco, fontePequena, (int)larguraPapel).Height + 2;

            g.DrawLine(Pens.Black, margemEsq, yPos, margemDir, yPos);
            yPos += 3;

            // 3. TÍTULO ABREVIADO
            g.DrawString("Doc. Auxiliar da Nota Fiscal", fonteNegrito, Brushes.Black, centro, yPos, formatoCentro);
            yPos += 10;
            g.DrawString("de Consumidor Eletronica", fonteNegrito, Brushes.Black, centro, yPos, formatoCentro);
            yPos += 10;

            g.DrawLine(Pens.Black, margemEsq, yPos, margemDir, yPos);
            yPos += 3;

            // 4. ITENS DA VENDA
            g.DrawString("COD   DESCRICAO", fontePequena, Brushes.Black, margemEsq, yPos);
            yPos += 10;
            g.DrawString("QTDxUN VL UN(R$) VL TOT(R$)", fontePequena, Brushes.Black, margemDir, yPos, formatoDireita);
            yPos += 10;
            g.DrawLine(Pens.Black, margemEsq, yPos, margemDir, yPos);
            yPos += 3;

            foreach (var det in _nfe.infNFe.det)
            {
                string produto = $"{det.prod.cProd} {det.prod.xProd}";
                RectangleF rectProd = new RectangleF(margemEsq, yPos, larguraPapel, 40);
                g.DrawString(produto, fonteNormal, Brushes.Black, rectProd, formatoEsquerda);

                yPos += g.MeasureString(produto, fonteNormal, (int)larguraPapel).Height;

                string valores = $"{det.prod.qCom:0.##} {det.prod.uCom}  {det.prod.vUnCom:N2}  {det.prod.vProd:N2}";
                g.DrawString(valores, fonteNormal, Brushes.Black, margemDir, yPos, formatoDireita);
                yPos += 12;
            }

            g.DrawLine(Pens.Black, margemEsq, yPos, margemDir, yPos);
            yPos += 3;

            // 5. TOTAIS
            g.DrawString("QTD TOTAL ITENS", fonteNormal, Brushes.Black, margemEsq, yPos);
            g.DrawString(_nfe.infNFe.det.Count.ToString(), fonteNormal, Brushes.Black, margemDir, yPos, formatoDireita);
            yPos += 12;

            g.DrawString("VALOR TOTAL R$", fonteTitulo, Brushes.Black, margemEsq, yPos);
            g.DrawString(_nfe.infNFe.total.ICMSTot.vNF.ToString("N2"), fonteTitulo, Brushes.Black, margemDir, yPos, formatoDireita);
            yPos += 15;

            // 6. FORMAS DE PAGAMENTO
            g.DrawString("PAGAMENTO", fonteNormal, Brushes.Black, margemEsq, yPos);
            g.DrawString("VALOR PAGO", fonteNormal, Brushes.Black, margemDir, yPos, formatoDireita);
            yPos += 12;

            foreach (var pag in _nfe.infNFe.pag)
            {
                foreach (var detPag in pag.detPag)
                {
                    string formaTexto = "Dinheiro";
                    if ((int)detPag.tPag == 17) formaTexto = "PIX";
                    if ((int)detPag.tPag == 3) formaTexto = "Credito";
                    if ((int)detPag.tPag == 4) formaTexto = "Debito";

                    g.DrawString(formaTexto, fonteNormal, Brushes.Black, margemEsq, yPos);
                    g.DrawString(detPag.vPag.ToString("N2"), fonteNormal, Brushes.Black, margemDir, yPos, formatoDireita);
                    yPos += 12;
                }
            }

            yPos += 3;
            g.DrawLine(Pens.Black, margemEsq, yPos, margemDir, yPos);
            yPos += 5;

            // 7. CHAVE DE ACESSO E MENSAGEM FINAL
            g.DrawString("Consulte a Chave de Acesso em:", fontePequena, Brushes.Black, centro, yPos, formatoCentro);
            yPos += 10;
            g.DrawString("nfce.fazenda.sp.gov.br/consulta", fontePequena, Brushes.Black, centro, yPos, formatoCentro);
            yPos += 12;

            string chave = _nfe.infNFe.Id.Replace("NFe", "");
            string chaveFormatada = "";
            for (int i = 0; i < chave.Length; i += 4)
            {
                if (i + 4 <= chave.Length) chaveFormatada += chave.Substring(i, 4) + " ";
                else chaveFormatada += chave.Substring(i);
            }

            RectangleF rectChave = new RectangleF(margemEsq, yPos, larguraPapel, 40);
            g.DrawString(chaveFormatada, fonteNegrito, Brushes.Black, rectChave, formatoCentro);

            yPos += g.MeasureString(chaveFormatada, fonteNegrito, (int)larguraPapel).Height + 5;

            g.DrawString("Obrigado e volte sempre!", fontePequena, Brushes.Black, centro, yPos, formatoCentro);

            // Folga da guilhotina
            yPos += 30;
        }
    }
}