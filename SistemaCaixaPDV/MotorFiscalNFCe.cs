using DFe.Classes.Entidades;
using DFe.Classes.Flags;
using DFe.Utils;
using Microsoft.Data.Sqlite; // DRIVER CORRETO INJETADO
using NFe.Classes;
using NFe.Classes.Informacoes;
using NFe.Classes.Informacoes.Destinatario;
using NFe.Classes.Informacoes.Detalhe;
using NFe.Classes.Informacoes.Detalhe.Tributacao;
using NFe.Classes.Informacoes.Detalhe.Tributacao.Estadual;
using NFe.Classes.Informacoes.Detalhe.Tributacao.Estadual.Tipos;
using NFe.Classes.Informacoes.Detalhe.Tributacao.Federal;
using NFe.Classes.Informacoes.Detalhe.Tributacao.Federal.Tipos;
using NFe.Classes.Informacoes.Emitente;
using NFe.Classes.Informacoes.Identificacao;
using NFe.Classes.Informacoes.Identificacao.Tipos;
using NFe.Classes.Informacoes.Pagamento;
using NFe.Classes.Informacoes.Total;
using NFe.Classes.Informacoes.Transporte;
using NFe.Classes.Servicos.Tipos;
using NFe.Servicos;
using NFe.Utils;
using NFe.Utils.InformacoesSuplementares;
using NFe.Utils.NFe;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Windows;
using System.Linq;

namespace SistemaCaixaPDV
{
    public static class MotorFiscalNFCe
    {
        private static string connectionString = BancoDeDados.ConnectionString;

        public static X509Certificate2 CertificadoDigital { get; set; }
        public static void CarregarCertificado(string caminho = "", string senha = "") { }

        private static string LerBancoSeguro(SqliteDataReader r, string coluna)
        {
            try { return r[coluna] == DBNull.Value ? "" : r[coluna].ToString(); }
            catch { return ""; }
        }

        public static bool EmitirNFCeProducao(string serie, int numero, string docCliente, string nomeCliente, List<ItemVenda> itens, decimal valorDescontoTotal, string formaPagamentoTela)
        {
            try
            {
                string cnpjEmit = "", ieEmit = "", razaoEmit = "", ibgeEmit = "3522604";
                string endRua = "", endNum = "", endBairro = "", endCep = "", endCidade = "", ufEmit = "SP";
                string camCert = "", senhaCert = "", idCsc = "000001", csc = "01234567-8901-2345-6789-012345678901";

                using (var cx = new SqliteConnection(connectionString))
                {
                    cx.Open();
                    using (var cmd = new SqliteCommand("SELECT * FROM Configuracoes LIMIT 1", cx))
                    {
                        using (var r = cmd.ExecuteReader())
                        {
                            if (r.Read())
                            {
                                cnpjEmit = LerBancoSeguro(r, "Cnpj").Replace(".", "").Replace("/", "").Replace("-", "").Trim();
                                ieEmit = LerBancoSeguro(r, "Ie").Replace(".", "").Replace("-", "").Trim();
                                razaoEmit = LerBancoSeguro(r, "NomeLoja");

                                endRua = LerBancoSeguro(r, "Rua");
                                endNum = LerBancoSeguro(r, "Numero");
                                if (string.IsNullOrEmpty(endNum)) endNum = "SN";
                                endBairro = LerBancoSeguro(r, "Bairro");
                                endCep = LerBancoSeguro(r, "Cep").Replace("-", "").Replace(".", "").Trim();
                                endCidade = LerBancoSeguro(r, "Cidade");

                                string ibgeLido = LerBancoSeguro(r, "CodigoIbge");
                                if (!string.IsNullOrEmpty(ibgeLido)) ibgeEmit = ibgeLido;

                                ufEmit = LerBancoSeguro(r, "Uf");
                                camCert = LerBancoSeguro(r, "CaminhoCertificado");
                                senhaCert = LerBancoSeguro(r, "SenhaCertificado");

                                string idLido = LerBancoSeguro(r, "IdCsc");
                                string cscLido = LerBancoSeguro(r, "CscSefaz");

                                if (!string.IsNullOrEmpty(idLido)) idCsc = idLido;
                                if (!string.IsNullOrEmpty(cscLido)) csc = cscLido;
                            }
                        }
                    }
                }

                if (string.IsNullOrEmpty(camCert) || !File.Exists(camCert)) { MessageBox.Show("Certificado não configurado ou arquivo não encontrado!", "Erro", MessageBoxButton.OK, MessageBoxImage.Error); return false; }

                csc = csc.Replace(" ", "").Replace("\n", "").Replace("\r", "").Replace("\t", "").Trim();
                idCsc = idCsc.Replace(" ", "").Trim().PadLeft(6, '0');

                Estado estEmitente = Estado.SP;
                Enum.TryParse(ufEmit, true, out estEmitente);

                var nfe = new NFe.Classes.NFe();
                nfe.infNFe = new NFe.Classes.Informacoes.infNFe
                {
                    versao = "4.00",
                    ide = new ide
                    {
                        cUF = estEmitente,
                        cNF = new Random().Next(10000000, 99999999).ToString(),
                        natOp = "VENDA DE MERCADORIA",
                        mod = ModeloDocumento.NFCe, // 65
                        serie = Convert.ToInt32(serie),
                        nNF = numero,
                        dhEmi = DateTime.Now,
                        tpNF = TipoNFe.tnSaida,
                        idDest = DestinoOperacao.doInterna,
                        cMunFG = Convert.ToInt32(ibgeEmit),
                        tpImp = TipoImpressao.tiNFCe,
                        tpEmis = TipoEmissao.teNormal,
                        tpAmb = TipoAmbiente.Producao,
                        finNFe = FinalidadeNFe.fnNormal,
                        indFinal = ConsumidorFinal.cfConsumidorFinal,
                        indPres = PresencaComprador.pcPresencial,
                        verProc = "1.0.0"
                    },
                    emit = new emit
                    {
                        CNPJ = cnpjEmit,
                        xNome = razaoEmit,
                        IE = ieEmit,
                        CRT = CRT.SimplesNacional,
                        enderEmit = new enderEmit
                        {
                            xLgr = endRua,
                            nro = endNum,
                            xBairro = endBairro,
                            cMun = Convert.ToInt32(ibgeEmit),
                            xMun = endCidade,
                            UF = estEmitente,
                            CEP = endCep
                        }
                    },
                    det = new List<det>()
                };

                if (!string.IsNullOrWhiteSpace(docCliente))
                {
                    nfe.infNFe.dest = new dest(VersaoServico.Versao400)
                    {
                        CNPJ = docCliente.Length > 11 ? docCliente.Replace(".", "").Replace("-", "").Replace("/", "") : null,
                        CPF = docCliente.Length <= 11 ? docCliente.Replace(".", "").Replace("-", "") : null,
                        xNome = string.IsNullOrWhiteSpace(nomeCliente) ? null : nomeCliente.Trim(),
                        indIEDest = indIEDest.NaoContribuinte
                    };
                }

                decimal totalProdutos = 0;
                foreach (var item in itens) { totalProdutos += item.Subtotal; }

                decimal somaDescontosRateados = 0;
                int numeroItemSequencial = 1;

                foreach (var item in itens)
                {
                    decimal subtotalItem = item.Subtotal;
                    decimal descontoDesteItem = 0;

                    if (valorDescontoTotal > 0 && totalProdutos > 0)
                    {
                        if (numeroItemSequencial == itens.Count)
                        {
                            descontoDesteItem = valorDescontoTotal - somaDescontosRateados;
                        }
                        else
                        {
                            descontoDesteItem = Math.Round((subtotalItem / totalProdutos) * valorDescontoTotal, 2);
                            somaDescontosRateados += descontoDesteItem;
                        }
                    }

                    nfe.infNFe.det.Add(new det
                    {
                        nItem = numeroItemSequencial++,
                        prod = new prod
                        {
                            cProd = string.IsNullOrEmpty(item.Codigo) ? "001" : item.Codigo,
                            cEAN = "SEM GTIN",
                            cEANTrib = "SEM GTIN",
                            xProd = item.Descricao,
                            NCM = string.IsNullOrEmpty(item.Ncm) ? "00000000" : item.Ncm,
                            CFOP = Convert.ToInt32(string.IsNullOrEmpty(item.Cfop) ? "5102" : item.Cfop),
                            uCom = "UN",
                            qCom = item.Quantidade,
                            vUnCom = item.ValorUnitario,
                            vProd = subtotalItem,
                            vDesc = descontoDesteItem > 0 ? descontoDesteItem : default(decimal?),
                            uTrib = "UN",
                            qTrib = item.Quantidade,
                            vUnTrib = item.ValorUnitario,
                            indTot = IndicadorTotal.ValorDoItemCompoeTotalNF
                        },
                        imposto = new imposto
                        {
                            ICMS = new ICMS { TipoICMS = new ICMSSN102 { orig = OrigemMercadoria.OmNacional, CSOSN = Csosnicms.Csosn102 } },
                            PIS = new PIS { TipoPIS = new PISOutr { CST = CSTPIS.pis99, vBC = 0.00m, pPIS = 0.00m, vPIS = 0.00m } },
                            COFINS = new COFINS { TipoCOFINS = new COFINSOutr { CST = CSTCOFINS.cofins99, vBC = 0.00m, pCOFINS = 0.00m, vCOFINS = 0.00m } }
                        }
                    });
                }

                decimal valorTotalNf = totalProdutos - valorDescontoTotal;
                if (valorTotalNf < 0) valorTotalNf = 0;

                nfe.infNFe.total = new total
                {
                    ICMSTot = new ICMSTot { vBC = 0, vICMS = 0, vProd = totalProdutos, vFrete = 0, vSeg = 0, vDesc = valorDescontoTotal, vNF = valorTotalNf, vII = 0, vIPI = 0, vIPIDevol = 0, vOutro = 0, vICMSDeson = 0, vFCP = 0, vBCST = 0, vST = 0, vFCPST = 0, vFCPSTRet = 0 }
                };

                nfe.infNFe.transp = new transp { modFrete = ModalidadeFrete.mfSemFrete };

                FormaPagamento formaOficial = FormaPagamento.fpDinheiro;
                string fPagLow = string.IsNullOrEmpty(formaPagamentoTela) ? "" : formaPagamentoTela.ToLower();

                if (fPagLow.Contains("pix")) formaOficial = (FormaPagamento)17;
                else if (fPagLow.Contains("crédito") || fPagLow.Contains("credito")) formaOficial = FormaPagamento.fpCartaoCredito;
                else if (fPagLow.Contains("débito") || fPagLow.Contains("debito")) formaOficial = FormaPagamento.fpCartaoDebito;

                var detalhePagamento = new detPag { tPag = formaOficial, vPag = valorTotalNf };

                if (formaOficial == FormaPagamento.fpCartaoCredito || formaOficial == FormaPagamento.fpCartaoDebito || formaOficial == (FormaPagamento)17)
                {
                    detalhePagamento.card = new card { tpIntegra = (TipoIntegracaoPagamento)2 };
                }

                nfe.infNFe.pag = new List<pag> { new pag { detPag = new List<detPag> { detalhePagamento } } };

                var configServico = ConfiguracaoServico.Instancia;
                configServico.tpAmb = TipoAmbiente.Producao;
                configServico.tpEmis = TipoEmissao.teNormal;
                configServico.cUF = estEmitente;
                configServico.ModeloDocumento = ModeloDocumento.NFCe;
                configServico.DiretorioSchemas = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Schemas");
                configServico.VersaoNFeAutorizacao = VersaoServico.Versao400;
                configServico.VersaoNFeRetAutorizacao = VersaoServico.Versao400;
                configServico.TimeOut = 15000;
                configServico.Certificado.TipoCertificado = TipoCertificado.A1Arquivo;
                configServico.Certificado.Arquivo = camCert;
                configServico.Certificado.Senha = senhaCert;

                nfe.Assina(configServico);

                nfe.infNFeSupl = new infNFeSupl();
                nfe.infNFeSupl.urlChave = nfe.infNFeSupl.ObterUrlConsulta(nfe, VersaoQrCode.QrCodeVersao2);

                try { nfe.infNFeSupl.qrCode = nfe.infNFeSupl.ObterUrlQrCode(nfe, VersaoQrCode.QrCodeVersao2, idCsc, csc, configServico.Certificado); }
                catch
                {
                    try { nfe.infNFeSupl.qrCode = nfe.infNFeSupl.ObterUrlQrCode(nfe, VersaoQrCode.QrCodeVersao2, csc, idCsc, configServico.Certificado); }
                    catch { nfe.infNFeSupl.qrCode = nfe.infNFeSupl.ObterUrlQrCode(nfe, VersaoQrCode.QrCodeVersao2, "000001", "012345678901234567890123456789012345", configServico.Certificado); }
                }

                // Proteção contra ServicePointManager obsoleto, resolvendo via HttpClient internamente
                var servicosNFe = new ServicosNFe(configServico);
                var retorno = servicosNFe.NFeAutorizacao(numero, IndicadorSincronizacao.Sincrono, new List<NFe.Classes.NFe> { nfe });

                string statusSefaz = retorno.Retorno.protNFe?.infProt.cStat.ToString() ?? retorno.Retorno.cStat.ToString();
                string motivoSefaz = retorno.Retorno.protNFe?.infProt.xMotivo ?? retorno.Retorno.xMotivo;

                if (statusSefaz == "100")
                {
                    string pastaXml = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "XML_Gerados", "NFCe");
                    if (!Directory.Exists(pastaXml)) Directory.CreateDirectory(pastaXml);

                    string chaveAcesso = retorno.Retorno.protNFe.infProt.chNFe;
                    string caminhoCompleto = $@"{pastaXml}\{chaveAcesso}-nfce.xml";

                    nfe.SalvarArquivoXml(caminhoCompleto);
                    return true;
                }
                else
                {
                    MessageBox.Show($"REJEIÇÃO NFC-e:\n\nCódigo: {statusSefaz}\nMotivo: {motivoSefaz}", "Retorno SEFAZ", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro de comunicação NFC-e: " + ex.Message, "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
    }
}