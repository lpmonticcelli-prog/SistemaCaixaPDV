using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;

namespace SistemaCaixaPDV
{
    public static class MotorFiscalACBr
    {
        // CORREÇÃO PONTO 2: Comunicação ACBr agora feita dentro da raiz do sistema para evitar bloqueio de permissão do Windows (UAC)
        private static string pastaAcbr = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ACBr_Comm");

        public static void EnviarVendaParaACBr(List<ItemVenda> itens, decimal totalVenda, string formaPagamento, string cpfCliente = "")
        {
            try
            {
                if (!Directory.Exists(pastaAcbr)) Directory.CreateDirectory(pastaAcbr);

                string arquivoEntrada = Path.Combine(pastaAcbr, "ENT.TXT");
                string arquivoSaida = Path.Combine(pastaAcbr, "SAI.TXT");

                StringBuilder ini = new StringBuilder();

                // COMANDO PARA O ACBR CRIAR A NOTA (NFC-e)
                ini.AppendLine("NFE.CriarEnviarNFe(");

                // --- CABEÇALHO DA NOTA ---
                ini.AppendLine("[infNFe]");
                ini.AppendLine("versao=4.00");

                ini.AppendLine("[Identificacao]");
                ini.AppendLine("natOp=VENDA DE MERCADORIA");
                ini.AppendLine("modFrete=9"); // Sem frete

                // Se tiver CPF, adiciona no cupom
                if (!string.IsNullOrEmpty(cpfCliente))
                {
                    ini.AppendLine("[Destinatario]");
                    ini.AppendLine($"CPF={cpfCliente}");
                }

                // --- ITENS DA VENDA ---
                for (int i = 0; i < itens.Count; i++)
                {
                    var item = itens[i];
                    string numeroItem = (i + 1).ToString("D3"); // 001, 002, 003...

                    ini.AppendLine($"[Produto{numeroItem}]");
                    ini.AppendLine($"cProd={item.Codigo}");
                    ini.AppendLine($"cEAN=SEM GTIN");
                    ini.AppendLine($"xProd={item.Descricao}");
                    ini.AppendLine($"NCM={item.Ncm}");
                    ini.AppendLine($"CFOP={item.Cfop}");
                    ini.AppendLine($"uCom=UN");
                    ini.AppendLine($"qCom={item.Quantidade.ToString("0.00").Replace(",", ".")}");
                    ini.AppendLine($"vUnCom={item.ValorUnitario.ToString("0.00").Replace(",", ".")}");
                    ini.AppendLine($"vProd={item.Subtotal.ToString("0.00").Replace(",", ".")}");
                    ini.AppendLine("indTot=1");

                    // Impostos (Simples Nacional) puxados lá do Cadastro de Produtos
                    ini.AppendLine($"[ICMS{numeroItem}]");
                    ini.AppendLine($"CSOSN={item.Csosn}");
                    ini.AppendLine("orig=0");
                }

                // --- PAGAMENTO ---
                string codPagamento = ConverterFormaPagtoSefaz(formaPagamento);
                ini.AppendLine("[Pagamento001]");
                ini.AppendLine($"tPag={codPagamento}");
                ini.AppendLine($"vPag={totalVenda.ToString("0.00").Replace(",", ".")}");

                // FECHA O COMANDO
                ini.AppendLine(", 1, 1)"); // 1 = Imprimir Danfe Automático, 1 = Síncrono

                // Apaga respostas antigas
                if (File.Exists(arquivoSaida)) File.Delete(arquivoSaida);

                // Grava o arquivo ENT.TXT para o ACBr ler
                File.WriteAllText(arquivoEntrada, ini.ToString(), Encoding.GetEncoding("Windows-1252"));

                // Aqui o sistema aguardaria a resposta do SAI.TXT para saber se aprovou.
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao comunicar com o ACBr: " + ex.Message, "Erro Fiscal", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Tabela oficial da SEFAZ para meios de pagamento
        private static string ConverterFormaPagtoSefaz(string formaPagto)
        {
            if (formaPagto.ToUpper().Contains("DINHEIRO")) return "01";
            if (formaPagto.ToUpper().Contains("CRÉDITO")) return "03";
            if (formaPagto.ToUpper().Contains("DÉBITO")) return "04";
            if (formaPagto.ToUpper().Contains("PIX")) return "17";
            if (formaPagto.ToUpper().Contains("CARNÊ")) return "05"; // Crédito Loja
            return "99"; // Outros
        }
    }
}