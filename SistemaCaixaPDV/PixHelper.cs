using System;
using System.Text;

namespace SistemaCaixaPDV // <-- ATENÇÃO: Verifique o nome do projeto!
{
    public static class PixHelper
    {
        public static string GerarPayloadPix(string chavePix, decimal valor, string nomeLoja, string cidade)
        {
            // Limpa os textos para evitar que caracteres especiais quebrem o padrão do banco
            chavePix = chavePix.Trim();
            nomeLoja = FormatarTexto(nomeLoja, 25);
            cidade = FormatarTexto(string.IsNullOrWhiteSpace(cidade) ? "Sua Cidade" : cidade, 15);

            string payloadFormatIndicator = "000201";

            string merchantAccountInfo = $"0014br.gov.bcb.pix01{chavePix.Length:D2}{chavePix}";
            merchantAccountInfo = $"26{merchantAccountInfo.Length:D2}{merchantAccountInfo}";

            string merchantCategoryCode = "52040000";
            string transactionCurrency = "5303986"; // 986 = Real Brasileiro (BRL)

            // O valor do PIX deve ter ponto ao invés de vírgula
            string strValor = valor.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
            string transactionAmount = $"54{strValor.Length:D2}{strValor}";

            string countryCode = "5802BR";
            string merchantName = $"59{nomeLoja.Length:D2}{nomeLoja}";
            string merchantCity = $"60{cidade.Length:D2}{cidade}";

            string additionalDataFieldTemplate = "62070503***"; // ID automático

            // Monta o código completo sem a assinatura
            string payload = $"{payloadFormatIndicator}{merchantAccountInfo}{merchantCategoryCode}{transactionCurrency}{transactionAmount}{countryCode}{merchantName}{merchantCity}{additionalDataFieldTemplate}6304";

            // Retorna o Payload + a assinatura criptografada (CRC16)
            return payload + GerarCrc16(payload);
        }

        private static string FormatarTexto(string texto, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(texto)) return "LOJA";
            string limpo = texto.Normalize(NormalizationForm.FormD);
            StringBuilder sb = new StringBuilder();
            foreach (char c in limpo)
            {
                if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }
            string final = sb.ToString().Normalize(NormalizationForm.FormC);
            return final.Length > maxLength ? final.Substring(0, maxLength) : final;
        }

        // Criptografia exigida pelo Banco Central para validar o código (CCITT-FALSE)
        private static string GerarCrc16(string payload)
        {
            ushort polinomio = 0x1021;
            ushort resultado = 0xFFFF;
            byte[] dados = Encoding.ASCII.GetBytes(payload);

            foreach (byte b in dados)
            {
                resultado ^= (ushort)(b << 8);
                for (int i = 0; i < 8; i++)
                {
                    if ((resultado & 0x8000) != 0)
                        resultado = (ushort)((resultado << 1) ^ polinomio);
                    else
                        resultado <<= 1;
                }
            }
            return resultado.ToString("X4");
        }
    }
}