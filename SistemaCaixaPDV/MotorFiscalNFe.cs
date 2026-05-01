using DFe.Classes.Entidades;
using DFe.Classes.Flags;
using DFe.Utils;
using Microsoft.Data.Sqlite; // DRIVER CORRETO INJETADO
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
using NFe.Utils.NFe;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Windows;

namespace SistemaCaixaPDV
{
    public static class MotorFiscalNFe
    {
        private static string connectionString = BancoDeDados.ConnectionString;

        private static string LerBancoSeguro(SqliteDataReader r, string coluna)
        {
            try { return r[coluna] == DBNull.Value ? "" : r[coluna].ToString(); }
            catch { return ""; }
        }

        public static bool EmitirNFeModelo55(
            string serie, int numeroNota, string cpfCnpj, string nomeCliente,
            string cep, string rua, string numeroEnd, string bairro, string cidade,
            string uf, string ieCliente, bool isentoIe, List<ItemNFe> carrinhoDeProdutos)
        {
            try
            {
                string emitCnpj = "", emitNome = "", emitIe = "", emitUfStr = "";
                string emitRua = "", emitNum = "", emitBairro = "", emitCidade = "", emitCep = "";
                string caminhoCertificado = "", senhaCertificado = "";

                using (var cx = new SqliteConnection(connectionString))
                {
                    cx.Open();
                    using (var cmd = new SqliteCommand("SELECT * FROM Configuracoes LIMIT 1", cx))
                    {
                        using (var r = cmd.ExecuteReader())
                        {
                            if (r.Read())
                            {
                                emitCnpj = LerBancoSeguro(r, "Cnpj").Replace(".", "").Replace("-", "").Replace("/", "");
                                emitNome = LerBancoSeguro(r, "NomeLoja");
                                emitIe = LerBancoSeguro(r, "Ie").Replace(".", "").Replace("-", "");
                                emitUfStr = LerBancoSeguro(r, "Uf");
                                emitRua = LerBancoSeguro(r, "Rua");
                                emitNum = LerBancoSeguro(r, "Numero");
                                emitBairro = LerBancoSeguro(r, "Bairro");
                                emitCidade = LerBancoSeguro(r, "Cidade");
                                emitCep = LerBancoSeguro(r, "Cep").Replace("-", "");

                                caminhoCertificado = LerBancoSeguro(r, "CaminhoCertificado");
                                senhaCertificado = LerBancoSeguro(r, "SenhaCertificado");
                            }
                        }
                    }
                }

                if (string.IsNullOrEmpty(caminhoCertificado) || !File.Exists(caminhoCertificado))
                {
                    MessageBox.Show("O ficheiro do Certificado Digital não foi encontrado!\n\nVá ao ecrã de Configurações, selecione o certificado novamente e clique em Salvar.", "Certificado Ausente", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                X509Certificate2 certificadoParaZeus = null;
                try
                {
                    // Blindagem moderna contra obsoletos X509
                    certificadoParaZeus = X509CertificateLoader.LoadPkcs12FromFile(caminhoCertificado, senhaCertificado);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("A senha do Certificado Digital está incorreta ou o ficheiro é inválido!\n\nDetalhes: " + ex.Message, "Erro no Certificado", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                string pastaSchemas = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Schemas");
                if (!Directory.Exists(pastaSchemas)) Directory.CreateDirectory(pastaSchemas);

                Estado estadoEmpresa = (Estado)Enum.Parse(typeof(Estado), string.IsNullOrEmpty(emitUfStr) ? "SP" : emitUfStr.ToUpper());

                ConfiguracaoServico.Instancia.DiretorioSchemas = pastaSchemas;
                ConfiguracaoServico.Instancia.cUF = estadoEmpresa;
                ConfiguracaoServico.Instancia.tpAmb = TipoAmbiente.Producao;
                ConfiguracaoServico.Instancia.tpEmis = TipoEmissao.teNormal;
                ConfiguracaoServico.Instancia.ModeloDocumento = ModeloDocumento.NFe;
                ConfiguracaoServico.Instancia.Certificado.TipoCertificado = TipoCertificado.A1Arquivo;
                ConfiguracaoServico.Instancia.Certificado.Arquivo = caminhoCertificado;
                ConfiguracaoServico.Instancia.Certificado.Senha = senhaCertificado;
                ConfiguracaoServico.Instancia.VersaoNFeAutorizacao = VersaoServico.Versao400;
                ConfiguracaoServico.Instancia.VersaoNFeRetAutorizacao = VersaoServico.Versao400;

                string codigoNumericoAleatorio = new Random().Next(10000000, 99999999).ToString();

                var nfe = new NFe.Classes.NFe { infNFe = new infNFe { versao = "4.00" } };

                nfe.infNFe.ide = new ide
                {
                    cUF = estadoEmpresa,
                    cNF = codigoNumericoAleatorio,
                    natOp = "VENDA DE MERCADORIA",
                    mod = ModeloDocumento.NFe,
                    serie = Convert.ToInt16(serie),
                    nNF = numeroNota,
                    tpNF = TipoNFe.tnSaida,
                    cMunFG = 3523404, // IBGE
                    tpImp = TipoImpressao.tiRetrato,
                    tpEmis = TipoEmissao.teNormal,
                    tpAmb = TipoAmbiente.Producao,
                    finNFe = FinalidadeNFe.fnNormal,
                    indFinal = ConsumidorFinal.cfConsumidorFinal,
                    indPres = PresencaComprador.pcPresencial,
                    dhEmi = DateTime.Now,
                    idDest = (uf.ToUpper() == emitUfStr.ToUpper()) ? DestinoOperacao.doInterna : DestinoOperacao.doInterestadual,
                    procEmi = ProcessoEmissao.peAplicativoContribuinte,
                    verProc = "1.0"
                };

                nfe.infNFe.emit = new emit
                {
                    CNPJ = emitCnpj,
                    xNome = emitNome,
                    IE = emitIe,
                    CRT = CRT.SimplesNacional,
                    enderEmit = new enderEmit
                    {
                        xLgr = string.IsNullOrEmpty(emitRua) ? "Rua" : emitRua,
                        nro = string.IsNullOrEmpty(emitNum) ? "SN" : emitNum,
                        xBairro = string.IsNullOrEmpty(emitBairro) ? "Centro" : emitBairro,
                        cMun = 3523404,
                        xMun = string.IsNullOrEmpty(emitCidade) ? "Itatiba" : emitCidade,
                        UF = estadoEmpresa,
                        CEP = string.IsNullOrEmpty(emitCep) ? "13251062" : emitCep
                    }
                };

                var dest = new dest(VersaoServico.Versao400) { xNome = nomeCliente.Trim() };

                string docLimpo = new string(cpfCnpj.Where(char.IsDigit).ToArray());

                if (docLimpo.Length == 14)
                {
                    dest.CNPJ = docLimpo;

                    if (isentoIe || (!string.IsNullOrEmpty(ieCliente) && ieCliente.ToUpper() == "ISENTO"))
                    {
                        dest.indIEDest = indIEDest.Isento;
                    }
                    else if (!string.IsNullOrEmpty(ieCliente))
                    {
                        dest.indIEDest = indIEDest.ContribuinteICMS;
                        dest.IE = new string(ieCliente.Where(char.IsDigit).ToArray());
                    }
                    else
                    {
                        dest.indIEDest = indIEDest.NaoContribuinte;
                    }
                }
                else
                {
                    dest.CPF = docLimpo;
                    dest.indIEDest = indIEDest.NaoContribuinte;
                }

                dest.enderDest = new enderDest
                {
                    xLgr = rua,
                    nro = numeroEnd,
                    xBairro = bairro,
                    cMun = 3523404,
                    xMun = cidade,
                    UF = uf.ToUpper(),
                    CEP = new string(cep.Where(char.IsDigit).ToArray())
                };
                nfe.infNFe.dest = dest;

                nfe.infNFe.det = new List<det>();
                int numeroItem = 1; decimal valorTotalProdutos = 0;

                foreach (var item in carrinhoDeProdutos)
                {
                    var detalhe = new det
                    {
                        nItem = numeroItem,
                        prod = new prod
                        {
                            cProd = numeroItem.ToString("D3"),
                            xProd = item.Descricao,
                            NCM = item.Ncm,
                            CFOP = 5102,
                            uCom = "UN",
                            qCom = item.Quantidade,
                            vUnCom = item.ValorUnitario,
                            vProd = item.Total,
                            uTrib = "UN",
                            qTrib = item.Quantidade,
                            vUnTrib = item.ValorUnitario,
                            indTot = (IndicadorTotal)1
                        },
                        imposto = new imposto
                        {
                            ICMS = new ICMS { TipoICMS = new ICMSSN102 { orig = (OrigemMercadoria)0, CSOSN = Csosnicms.Csosn102 } },
                            PIS = new PIS { TipoPIS = new PISOutr { CST = CSTPIS.pis99, vBC = 0, pPIS = 0, vPIS = 0 } },
                            COFINS = new COFINS { TipoCOFINS = new COFINSOutr { CST = CSTCOFINS.cofins99, vBC = 0, pCOFINS = 0, vCOFINS = 0 } }
                        }
                    };
                    nfe.infNFe.det.Add(detalhe); valorTotalProdutos += item.Total; numeroItem++;
                }

                var icmsTot = new ICMSTot
                {
                    vBC = 0.00m,
                    vICMS = 0.00m,
                    vICMSDeson = 0.00m,
                    vFCP = 0.00m,
                    vBCST = 0.00m,
                    vST = 0.00m,
                    vFCPST = 0.00m,
                    vFCPSTRet = 0.00m,
                    vProd = valorTotalProdutos,
                    vFrete = 0.00m,
                    vSeg = 0.00m,
                    vDesc = 0.00m,
                    vII = 0.00m,
                    vIPI = 0.00m,
                    vIPIDevol = 0.00m,
                    vPIS = 0.00m,
                    vCOFINS = 0.00m,
                    vOutro = 0.00m,
                    vNF = valorTotalProdutos
                };

                foreach (var prop in icmsTot.GetType().GetProperties())
                {
                    if (prop.Name.EndsWith("Specified") && prop.PropertyType == typeof(bool) && prop.CanWrite)
                    {
                        prop.SetValue(icmsTot, true, null);
                    }
                }

                nfe.infNFe.total = new total { ICMSTot = icmsTot };

                nfe.infNFe.pag = new List<pag>
                {
                    new pag { detPag = new List<detPag> { new detPag { tPag = FormaPagamento.fpDinheiro, vPag = valorTotalProdutos, indPag = (IndicadorPagamentoDetalhePagamento)0 } } }
                };

                nfe.infNFe.transp = new transp { modFrete = ModalidadeFrete.mfSemFrete };

                nfe.Assina();

                var servicoNFe = new ServicosNFe(ConfiguracaoServico.Instancia, certificadoParaZeus);
                var retornoEnvio = servicoNFe.NFeAutorizacao(1, IndicadorSincronizacao.Sincrono, new List<NFe.Classes.NFe> { nfe }, false);

                if (retornoEnvio.Retorno.protNFe != null && retornoEnvio.Retorno.protNFe.infProt.cStat == 100)
                {
                    string pastaNotas = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "NotasFiscais");
                    if (!Directory.Exists(pastaNotas)) Directory.CreateDirectory(pastaNotas);

                    var nfeProc = new NFe.Classes.nfeProc { NFe = nfe, protNFe = retornoEnvio.Retorno.protNFe, versao = "4.00" };
                    string caminhoXml = Path.Combine(pastaNotas, $"NFe_{nfe.infNFe.Id}.xml");
                    nfeProc.SalvarArquivoXml(caminhoXml);

                    GerarDanfeSimplesHTML(nfeProc, Path.Combine(pastaNotas, $"DANFE_{nfe.infNFe.Id}.html"));
                    return true;
                }
                else
                {
                    string erroSefaz = "Erro desconhecido";
                    if (retornoEnvio.Retorno.protNFe != null && !string.IsNullOrEmpty(retornoEnvio.Retorno.protNFe.infProt.xMotivo))
                    {
                        erroSefaz = $"{retornoEnvio.Retorno.protNFe.infProt.cStat} - {retornoEnvio.Retorno.protNFe.infProt.xMotivo}";
                    }
                    else
                    {
                        erroSefaz = $"{retornoEnvio.Retorno.cStat} - {retornoEnvio.Retorno.xMotivo}";
                    }

                    MessageBox.Show($"A SEFAZ rejeitou a NF-e.\n\nMotivo Real: {erroSefaz}", "Rejeição SEFAZ", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro Crítico ao processar a NF-e: " + ex.Message, "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private static void GerarDanfeSimplesHTML(NFe.Classes.nfeProc notaAprovada, string caminhoArquivoHtml)
        {
            try
            {
                var nfe = notaAprovada.NFe.infNFe;
                string chaveAcesso = notaAprovada.protNFe.infProt.chNFe;
                string protocolo = notaAprovada.protNFe.infProt.nProt;

                string linhasProdutos = "";
                foreach (var det in nfe.det)
                {
                    linhasProdutos += $@"
                        <tr>
                            <td style='border: 1px solid #ddd; padding: 8px;'>{det.prod.cProd}</td>
                            <td style='border: 1px solid #ddd; padding: 8px;'>{det.prod.xProd}</td>
                            <td style='border: 1px solid #ddd; padding: 8px;'>{det.prod.NCM}</td>
                            <td style='border: 1px solid #ddd; padding: 8px;'>{det.prod.qCom:N4}</td>
                            <td style='border: 1px solid #ddd; padding: 8px;'>R$ {det.prod.vUnCom:N2}</td>
                            <td style='border: 1px solid #ddd; padding: 8px; font-weight: bold;'>R$ {det.prod.vProd:N2}</td>
                        </tr>";
                }

                string html = $@"
                <!DOCTYPE html>
                <html lang='pt-BR'>
                <head>
                    <meta charset='UTF-8'>
                    <title>DANFE Simples - {nfe.ide.nNF}</title>
                    <style>
                        body {{ font-family: Arial, sans-serif; margin: 40px; color: #333; }}
                        .container {{ border: 2px solid #000; padding: 20px; border-radius: 8px; max-width: 900px; margin: auto; }}
                        .header {{ text-align: center; border-bottom: 2px solid #000; padding-bottom: 10px; margin-bottom: 20px; }}
                        .box {{ border: 1px solid #000; padding: 10px; border-radius: 5px; margin-bottom: 15px; }}
                        h1, h2, h3, p {{ margin: 5px 0; }}
                        table {{ width: 100%; border-collapse: collapse; margin-top: 15px; }}
                        th {{ background-color: #f2f2f2; border: 1px solid #ddd; padding: 8px; text-align: left; }}
                        .btn-print {{ display: block; width: 250px; margin: 20px auto; padding: 12px; text-align: center; background: #2563EB; color: white; text-decoration: none; font-weight: bold; border-radius: 5px; cursor: pointer; border: none; font-size: 16px; }}
                        .btn-print:hover {{ background: #1D4ED8; }}
                        @media print {{ .btn-print {{ display: none; }} }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>DOCUMENTO AUXILIAR DA NOTA FISCAL ELETRÔNICA (DANFE)</h1>
                            <h2>{nfe.emit.xNome}</h2>
                            <p>CNPJ: {nfe.emit.CNPJ} | IE: {nfe.emit.IE}</p>
                        </div>

                        <div class='box'>
                            <h3>DADOS DA NOTA FISCAL</h3>
                            <p><strong>Número:</strong> {nfe.ide.nNF} &nbsp;&nbsp;&nbsp; <strong>Série:</strong> {nfe.ide.serie}</p>
                            <p><strong>Chave de Acesso:</strong> {chaveAcesso}</p>
                            <p><strong>Protocolo de Autorização:</strong> {protocolo}</p>
                        </div>

                        <div class='box'>
                            <h3>DESTINATÁRIO</h3>
                            <p><strong>Nome:</strong> {nfe.dest.xNome}</p>
                            <p><strong>CPF/CNPJ:</strong> {(string.IsNullOrEmpty(nfe.dest.CNPJ) ? nfe.dest.CPF : nfe.dest.CNPJ)}</p>
                            <p><strong>Endereço:</strong> {nfe.dest.enderDest.xLgr}, {nfe.dest.enderDest.nro} - {nfe.dest.enderDest.xMun}/{nfe.dest.enderDest.UF}</p>
                        </div>

                        <h3>PRODUTOS</h3>
                        <table>
                            <thead>
                                <tr>
                                    <th>Cód.</th>
                                    <th>Descrição do Produto</th>
                                    <th>NCM</th>
                                    <th>Qtd.</th>
                                    <th>V. Unitário</th>
                                    <th>V. Total</th>
                                </tr>
                            </thead>
                            <tbody>
                                {linhasProdutos}
                            </tbody>
                        </table>

                        <div class='box' style='margin-top: 20px; text-align: right; background-color: #f9fafb;'>
                            <h2>TOTAL DA NOTA: R$ {nfe.total.ICMSTot.vNF:N2}</h2>
                        </div>
                    </div>
                    
                    <button class='btn-print' onclick='window.print()'>🖨️ IMPRIMIR / SALVAR PDF</button>
                </body>
                </html>";

                File.WriteAllText(caminhoArquivoHtml, html);

                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = caminhoArquivoHtml,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("A nota foi emitida, mas ocorreu um erro ao gerar a visualização: " + ex.Message, "Aviso Visualização", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}