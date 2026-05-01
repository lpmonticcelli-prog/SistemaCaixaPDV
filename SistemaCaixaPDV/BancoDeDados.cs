using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using System.IO;
using System.Windows;

namespace SistemaCaixaPDV
{
    public static class BancoDeDados
    {
        // =========================================================================================
        // ARQUITETURA DE PRODUÇÃO: Redirecionamento para ProgramData (Evita bloqueio de UAC/Permissão)
        // =========================================================================================
        public static readonly string PastaDados = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "SistemaCaixaPDV");
        public static readonly string CaminhoBanco = Path.Combine(PastaDados, "bancopdv_v3.sqlite");
        public static string ConnectionString => $"Data Source={CaminhoBanco};";

        public static void InicializarBanco()
        {
            try
            {
                // Garante que a pasta existe antes de tentar criar o banco
                if (!Directory.Exists(PastaDados)) Directory.CreateDirectory(PastaDados);

                using (var conexao = new SqliteConnection(ConnectionString))
                {
                    conexao.Open();
                    string[] comandos = {
                        @"CREATE TABLE IF NOT EXISTS Produtos (Codigo TEXT PRIMARY KEY, Descricao TEXT NOT NULL, Preco NUMERIC NOT NULL, Unidade TEXT, PrecoCompra NUMERIC, Margem NUMERIC, PrecoPrazo NUMERIC, EstoqueAtual NUMERIC, Observacoes TEXT, CaminhoFoto TEXT, Ncm TEXT, Cfop TEXT, Csosn TEXT)",
                        @"CREATE TABLE IF NOT EXISTS Clientes (Id INTEGER PRIMARY KEY AUTOINCREMENT, Nome TEXT NOT NULL, CpfCnpj TEXT, Rg TEXT, Telefone TEXT, Celular TEXT, Email TEXT, Cep TEXT, Endereco TEXT, Numero TEXT, Complemento TEXT, Bairro TEXT, Cidade TEXT, Uf TEXT, Tipo TEXT, Observacoes TEXT, DataCadastro TEXT, TelefoneFixo TEXT)",
                        @"CREATE TABLE IF NOT EXISTS Vendas (Id INTEGER PRIMARY KEY AUTOINCREMENT, TotalLiquido NUMERIC, FormaPagamento TEXT, DataHora TEXT, NumeroVenda TEXT, DataVenda TEXT, ClienteId INTEGER, ClienteNome TEXT, Tipo TEXT, TotalBruto NUMERIC, TotalDesconto NUMERIC)",
                        @"CREATE TABLE IF NOT EXISTS ItensVenda (Id INTEGER PRIMARY KEY AUTOINCREMENT, VendaId INTEGER, CodigoProduto TEXT, Descricao TEXT, Quantidade NUMERIC, PrecoUnitario NUMERIC, Total NUMERIC)",
                        @"CREATE TABLE IF NOT EXISTS Despesas (Id INTEGER PRIMARY KEY AUTOINCREMENT, Descricao TEXT, Categoria TEXT, Valor NUMERIC, DataVencimento TEXT, Status TEXT)",
                        @"CREATE TABLE IF NOT EXISTS ContasReceber (Id INTEGER PRIMARY KEY AUTOINCREMENT, ClienteId INTEGER, ClienteNome TEXT, Descricao TEXT, Valor NUMERIC, DataVencimento TEXT, Status TEXT, DataPagamento TEXT, FormaPagamento TEXT, TipoDocumento TEXT)",
                        @"CREATE TABLE IF NOT EXISTS MovimentacoesCaixa (Id INTEGER PRIMARY KEY AUTOINCREMENT, Tipo TEXT, Valor NUMERIC, Motivo TEXT, DataHora TEXT)",
                        @"CREATE TABLE IF NOT EXISTS Configuracoes (Id INTEGER PRIMARY KEY, NomeLoja TEXT, AceitaPix INTEGER, AceitaCredito INTEGER, AceitaDebito INTEGER, TipoImpressora TEXT, SenhaGerente TEXT, ChavePix TEXT, CaminhoBanner TEXT)",
                        @"CREATE TABLE IF NOT EXISTS ControleNFe (Id INTEGER PRIMARY KEY, Serie INTEGER, Numero INTEGER)",
                        @"CREATE TABLE IF NOT EXISTS HistoricoCompras (Id INTEGER PRIMARY KEY AUTOINCREMENT, CodigoProduto TEXT, DataCompra TEXT, Fornecedor TEXT, NumeroNFe TEXT, Quantidade NUMERIC, CustoUnitario NUMERIC, Total NUMERIC)",
                        @"CREATE TABLE IF NOT EXISTS HistoricoEstoque (Id INTEGER PRIMARY KEY AUTOINCREMENT, CodigoProduto TEXT, DataHora TEXT, Tipo TEXT, Quantidade NUMERIC, Motivo TEXT)",
                        @"CREATE TABLE IF NOT EXISTS ProdutoVariacoes (Id INTEGER PRIMARY KEY AUTOINCREMENT, CodigoProduto TEXT, Atributo TEXT, Valor TEXT, CodigoBarras TEXT, Estoque NUMERIC)",
                        @"CREATE TABLE IF NOT EXISTS OrdemServico (Id INTEGER PRIMARY KEY AUTOINCREMENT, DataEntrada TEXT, DataSaida TEXT, Status TEXT, Problema TEXT, Laudo TEXT, Imei TEXT, Equipamento TEXT, MarcaModelo TEXT, Total NUMERIC)",
                        @"CREATE TABLE IF NOT EXISTS OrdemServicoItens (Id INTEGER PRIMARY KEY AUTOINCREMENT, OS_ID INTEGER, CodigoProduto TEXT, Quantidade NUMERIC, ValorUnitario NUMERIC, Subtotal NUMERIC, FOREIGN KEY(OS_ID) REFERENCES OrdemServico(Id))"
                    };

                    foreach (var sql in comandos)
                    {
                        using (var cmd = new SqliteCommand(sql, conexao))
                        {
                            cmd.ExecuteNonQuery();
                        }
                    }

                    try
                    {
                        using (var cmdCol = new SqliteCommand("ALTER TABLE Clientes ADD COLUMN CreditoLiberado NUMERIC DEFAULT 0", conexao))
                            cmdCol.ExecuteNonQuery();
                    }
                    catch { }
                }
            }
            catch { }
        }

        public static bool SalvarProdutoCompleto(string cod, string desc, string unid, decimal pCompra, decimal margem, decimal pVenda, decimal pPrazo, int estMin, string obs, string caminhoFotoAtual)
        {
            try
            {
                using (var cx = new SqliteConnection(ConnectionString))
                {
                    cx.Open();
                    string sql = @"INSERT INTO Produtos (Codigo, Descricao, Unidade, PrecoCompra, Margem, Preco, PrecoPrazo, EstoqueAtual, Observacoes, CaminhoFoto)
                                   VALUES (@cod, @desc, @unid, @pCompra, @margem, @pVenda, @pPrazo, @est, @obs, @foto)
                                   ON CONFLICT(Codigo) DO UPDATE SET
                                   Descricao = @desc, Unidade = @unid, PrecoCompra = @pCompra, Margem = @margem, Preco = @pVenda, PrecoPrazo = @pPrazo, EstoqueAtual = @est, Observacoes = @obs, CaminhoFoto = @foto;";

                    using (var cmd = new SqliteCommand(sql, cx))
                    {
                        cmd.Parameters.AddWithValue("@cod", cod);
                        cmd.Parameters.AddWithValue("@desc", desc);
                        cmd.Parameters.AddWithValue("@unid", unid ?? "UN");
                        cmd.Parameters.AddWithValue("@pCompra", pCompra);
                        cmd.Parameters.AddWithValue("@margem", margem);
                        cmd.Parameters.AddWithValue("@pVenda", pVenda);
                        cmd.Parameters.AddWithValue("@pPrazo", pPrazo);
                        cmd.Parameters.AddWithValue("@est", estMin);
                        cmd.Parameters.AddWithValue("@obs", obs ?? "");
                        cmd.Parameters.AddWithValue("@foto", caminhoFotoAtual ?? "");
                        cmd.ExecuteNonQuery();
                    }
                }
                return true;
            }
            catch (Exception ex) { MessageBox.Show("Erro ao salvar produto no banco: " + ex.Message, "Erro SQL", MessageBoxButton.OK, MessageBoxImage.Error); return false; }
        }

        public static Configuracao ObterConfiguracoes()
        {
            var conf = new Configuracao { NomeLoja = "VIWE Systems", TipoImpressora = "58mm" };
            try
            {
                using (var cx = new SqliteConnection(ConnectionString))
                {
                    cx.Open();
                    using (var cmd = new SqliteCommand("SELECT * FROM Configuracoes WHERE Id = 1", cx))
                    using (var r = cmd.ExecuteReader())
                    {
                        if (r.Read())
                        {
                            conf.NomeLoja = r["NomeLoja"]?.ToString() ?? "";
                            conf.SenhaGerente = r["SenhaGerente"]?.ToString() ?? "";
                            conf.TipoImpressora = r["TipoImpressora"]?.ToString() ?? "58mm";
                            conf.AceitaPix = r["AceitaPix"]?.ToString() == "1";
                            conf.ChavePix = r["ChavePix"]?.ToString() ?? "";
                        }
                    }
                }
            }
            catch { }
            return conf;
        }

        public static int ObterProximoNumeroVenda()
        {
            try { using (var cx = new SqliteConnection(ConnectionString)) { cx.Open(); using (var cmd = new SqliteCommand("SELECT MAX(Id) FROM Vendas", cx)) { var res = cmd.ExecuteScalar(); return (res != DBNull.Value && res != null) ? Convert.ToInt32(res) + 1 : 1; } } }
            catch { return 1; }
        }

        public static List<Cliente> ListarClientes()
        {
            var lista = new List<Cliente>();
            try
            {
                using (var cx = new SqliteConnection(ConnectionString))
                {
                    cx.Open();
                    using (var cmd = new SqliteCommand("SELECT Id, Nome, CpfCnpj FROM Clientes ORDER BY Nome", cx))
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            lista.Add(new Cliente
                            {
                                Id = Convert.ToInt32(r["Id"]),
                                Nome = r["Nome"]?.ToString() ?? "",
                                Cpf = r["CpfCnpj"]?.ToString() ?? ""
                            });
                        }
                    }
                }
            }
            catch { }
            return lista;
        }

        public static List<string> ListarNomesClientes()
        {
            var lista = new List<string>();
            try
            {
                using (var cx = new SqliteConnection(ConnectionString))
                {
                    cx.Open();
                    using (var cmd = new SqliteCommand("SELECT Nome FROM Clientes ORDER BY Nome", cx))
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            string nome = r.IsDBNull(0) ? string.Empty : r.GetString(0);
                            lista.Add(nome);
                        }
                    }
                }
            }
            catch { }
            return lista;
        }

        public static Produto BuscarProduto(string cod)
        {
            try
            {
                using (var cx = new SqliteConnection(ConnectionString))
                {
                    cx.Open();
                    using (var cmd = new SqliteCommand("SELECT Descricao, Preco FROM Produtos WHERE Codigo=@c", cx))
                    {
                        cmd.Parameters.AddWithValue("@c", cod);
                        using (var r = cmd.ExecuteReader())
                        {
                            if (r.Read())
                            {
                                return new Produto
                                {
                                    Codigo = cod,
                                    Descricao = r.IsDBNull(0) ? "" : r.GetString(0),
                                    Preco = r.GetDecimal(1)
                                };
                            }
                        }
                    }
                }
            }
            catch { }
            return null!;
        }

        public static List<Produto> BuscarProdutosPorNome(string termo)
        {
            var lista = new List<Produto>();
            try
            {
                using (var cx = new SqliteConnection(ConnectionString))
                {
                    cx.Open();
                    string sql = "SELECT Codigo, Descricao, Preco FROM Produtos WHERE Descricao LIKE @t OR Codigo LIKE @t ORDER BY Descricao";
                    using (var cmd = new SqliteCommand(sql, cx))
                    {
                        cmd.Parameters.AddWithValue("@t", "%" + termo + "%");
                        using (var r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                lista.Add(new Produto
                                {
                                    Codigo = r["Codigo"]?.ToString() ?? "",
                                    Descricao = r["Descricao"]?.ToString() ?? "",
                                    Preco = Convert.ToDecimal(r["Preco"])
                                });
                            }
                        }
                    }
                }
            }
            catch { }
            return lista;
        }

        public static void SalvarVariacoesProduto(string codigoProduto, List<ProdutoVariacaoModel> variacoes)
        {
            try
            {
                using (var cx = new SqliteConnection(ConnectionString))
                {
                    cx.Open();

                    using (var cmdDel = new SqliteCommand("DELETE FROM ProdutoVariacoes WHERE CodigoProduto = @c", cx))
                    {
                        cmdDel.Parameters.AddWithValue("@c", codigoProduto);
                        cmdDel.ExecuteNonQuery();
                    }

                    if (variacoes != null && variacoes.Count > 0)
                    {
                        string sqlIns = "INSERT INTO ProdutoVariacoes (CodigoProduto, Atributo, Valor, CodigoBarras, Estoque) VALUES (@cp, @at, @val, @cb, @est)";
                        foreach (var v in variacoes)
                        {
                            using (var cmdIns = new SqliteCommand(sqlIns, cx))
                            {
                                cmdIns.Parameters.AddWithValue("@cp", codigoProduto);
                                cmdIns.Parameters.AddWithValue("@at", v.Atributo ?? "");
                                cmdIns.Parameters.AddWithValue("@val", v.Valor ?? "");
                                cmdIns.Parameters.AddWithValue("@cb", v.CodigoBarras ?? "");
                                cmdIns.Parameters.AddWithValue("@est", v.Estoque);
                                cmdIns.ExecuteNonQuery();
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Erro ao salvar a Grade/Variação: " + ex.Message); }
        }

        public static List<ProdutoVariacaoModel> ObterVariacoesProduto(string codigoProduto)
        {
            var lista = new List<ProdutoVariacaoModel>();
            try
            {
                using (var cx = new SqliteConnection(ConnectionString))
                {
                    cx.Open();
                    using (var cmd = new SqliteCommand("SELECT * FROM ProdutoVariacoes WHERE CodigoProduto = @c", cx))
                    {
                        cmd.Parameters.AddWithValue("@c", codigoProduto);
                        using (var r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                lista.Add(new ProdutoVariacaoModel
                                {
                                    Id = Convert.ToInt32(r["Id"]),
                                    CodigoProduto = r["CodigoProduto"]?.ToString() ?? "",
                                    Atributo = r["Atributo"]?.ToString() ?? "",
                                    Valor = r["Valor"]?.ToString() ?? "",
                                    CodigoBarras = r["CodigoBarras"]?.ToString() ?? "",
                                    Estoque = r["Estoque"] == DBNull.Value ? 0 : Convert.ToDecimal(r["Estoque"])
                                });
                            }
                        }
                    }
                }
            }
            catch { }
            return lista;
        }

        public static List<RelatorioClienteModel> ObterRelatorioClientes()
        {
            var lista = new List<RelatorioClienteModel>();
            try
            {
                using (var cx = new SqliteConnection(ConnectionString))
                {
                    cx.Open();
                    using (var cmd = new SqliteCommand("SELECT Id, Nome, CpfCnpj, Celular, Cidade, Uf FROM Clientes ORDER BY Nome", cx))
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            lista.Add(new RelatorioClienteModel
                            {
                                Codigo = r.GetInt32(0).ToString("D3"),
                                Nome = r.IsDBNull(1) ? "" : r.GetString(1),
                                CPF = r.IsDBNull(2) ? "" : r.GetString(2),
                                Celular = r.IsDBNull(3) ? "" : r.GetString(3),
                                Cidade = r.IsDBNull(4) ? "" : r.GetString(4),
                                UF = r.IsDBNull(5) ? "" : r.GetString(5)
                            });
                        }
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Erro ao carregar clientes: " + ex.Message, "Erro BD", MessageBoxButton.OK, MessageBoxImage.Error); }
            return lista;
        }

        public static List<RelatorioProdutoModel> ObterRelatorioProdutos()
        {
            var lista = new List<RelatorioProdutoModel>();
            try
            {
                using (var cx = new SqliteConnection(ConnectionString))
                {
                    cx.Open();
                    using (var cmd = new SqliteCommand("SELECT Codigo, Descricao, Unidade, Preco, EstoqueAtual FROM Produtos ORDER BY Descricao", cx))
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            lista.Add(new RelatorioProdutoModel
                            {
                                Codigo = r.IsDBNull(0) ? "" : r.GetString(0),
                                Descricao = r.IsDBNull(1) ? "" : r.GetString(1),
                                Unidade = r.IsDBNull(2) ? "UN" : r.GetString(2),
                                PrecoVenda = r.GetDecimal(3).ToString("C"),
                                Estoque = r.IsDBNull(4) ? "0" : r.GetDecimal(4).ToString("N2")
                            });
                        }
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Erro ao carregar produtos: " + ex.Message, "Erro BD", MessageBoxButton.OK, MessageBoxImage.Error); }
            return lista;
        }

        public static List<RelatorioVendaModel> ObterRelatorioVendas(string dataInicio, string dataFim, out decimal somaTotal)
        {
            var lista = new List<RelatorioVendaModel>();
            somaTotal = 0;
            try
            {
                using (var cx = new SqliteConnection(ConnectionString))
                {
                    cx.Open();
                    string sql = "SELECT Id, DataHora, FormaPagamento, TotalLiquido FROM Vendas WHERE DataHora >= @inicio AND DataHora <= @fim ORDER BY DataHora DESC";
                    using (var cmd = new SqliteCommand(sql, cx))
                    {
                        cmd.Parameters.AddWithValue("@inicio", dataInicio); cmd.Parameters.AddWithValue("@fim", dataFim);
                        using (var r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                decimal valor = r.GetDecimal(3);
                                somaTotal += valor;

                                string dataBonita = r.IsDBNull(1) ? "" : r.GetString(1);
                                if (DateTime.TryParse(dataBonita, out DateTime dtParsed)) dataBonita = dtParsed.ToString("dd/MM/yyyy HH:mm");

                                lista.Add(new RelatorioVendaModel
                                {
                                    NumVenda = r.GetInt32(0).ToString("D4"),
                                    DataVenda = dataBonita,
                                    FormaPagto = r.IsDBNull(2) ? "" : r.GetString(2),
                                    ValorTotal = valor.ToString("C")
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Erro ao carregar vendas: " + ex.Message, "Erro BD", MessageBoxButton.OK, MessageBoxImage.Error); }
            return lista;
        }

        public static void SalvarDespesa(int id, string descricao, string categoria, decimal valor, string dataVencimento, string status)
        {
            try
            {
                using (var cx = new SqliteConnection(ConnectionString))
                {
                    cx.Open();
                    if (id == 0)
                    {
                        string sql = "INSERT INTO Despesas (Descricao, Categoria, Valor, DataVencimento, Status) VALUES (@d, @c, @v, @data, @s)";
                        using (var cmd = new SqliteCommand(sql, cx)) { cmd.Parameters.AddWithValue("@d", descricao); cmd.Parameters.AddWithValue("@c", categoria); cmd.Parameters.AddWithValue("@v", valor); cmd.Parameters.AddWithValue("@data", dataVencimento); cmd.Parameters.AddWithValue("@s", status); cmd.ExecuteNonQuery(); }
                    }
                    else
                    {
                        string sql = "UPDATE Despesas SET Descricao=@d, Categoria=@c, Valor=@v, DataVencimento=@data, Status=@s WHERE Id=@id";
                        using (var cmd = new SqliteCommand(sql, cx)) { cmd.Parameters.AddWithValue("@d", descricao); cmd.Parameters.AddWithValue("@c", categoria); cmd.Parameters.AddWithValue("@v", valor); cmd.Parameters.AddWithValue("@data", dataVencimento); cmd.Parameters.AddWithValue("@s", status); cmd.Parameters.AddWithValue("@id", id); cmd.ExecuteNonQuery(); }
                    }
                }
            }
            catch { }
        }

        public static void ExcluirDespesa(int id)
        {
            try { using (var cx = new SqliteConnection(ConnectionString)) { cx.Open(); using (var cmd = new SqliteCommand("DELETE FROM Despesas WHERE Id = @id", cx)) { cmd.Parameters.AddWithValue("@id", id); cmd.ExecuteNonQuery(); } } } catch { }
        }

        public static List<DespesaModel> FiltrarDespesas(string dataIni, string dataFim, string statusFiltro, out decimal totalFiltro)
        {
            var lista = new List<DespesaModel>();
            totalFiltro = 0;
            try
            {
                using (var cx = new SqliteConnection(ConnectionString))
                {
                    cx.Open();
                    string sql = "SELECT * FROM Despesas WHERE DataVencimento >= @ini AND DataVencimento <= @fim";
                    if (statusFiltro != "Todas") sql += " AND Status = @status";
                    sql += " ORDER BY DataVencimento ASC";

                    using (var cmd = new SqliteCommand(sql, cx))
                    {
                        cmd.Parameters.AddWithValue("@ini", dataIni); cmd.Parameters.AddWithValue("@fim", dataFim);
                        if (statusFiltro != "Todas") cmd.Parameters.AddWithValue("@status", statusFiltro);

                        using (var r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                decimal valor = r["Valor"] == DBNull.Value ? 0 : Convert.ToDecimal(r["Valor"]);
                                totalFiltro += valor;

                                string dataBanco = r["DataVencimento"]?.ToString() ?? "";
                                string dataExibicao = dataBanco;
                                if (DateTime.TryParse(dataBanco, out DateTime dt)) dataExibicao = dt.ToString("dd/MM/yyyy");

                                lista.Add(new DespesaModel
                                {
                                    Id = Convert.ToInt32(r["Id"]),
                                    Descricao = r["Descricao"]?.ToString() ?? "",
                                    Categoria = r["Categoria"]?.ToString() ?? "",
                                    ValorReais = valor.ToString("C"),
                                    DataFormatada = dataExibicao,
                                    Status = r["Status"]?.ToString() ?? ""
                                });
                            }
                        }
                    }
                }
            }
            catch { }
            return lista;
        }

        public static DespesaModel ObterDespesaPorId(int id)
        {
            try
            {
                using (var cx = new SqliteConnection(ConnectionString))
                {
                    cx.Open();
                    using (var cmd = new SqliteCommand("SELECT * FROM Despesas WHERE Id = @id", cx))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        using (var r = cmd.ExecuteReader())
                        {
                            if (r.Read())
                            {
                                return new DespesaModel
                                {
                                    Id = Convert.ToInt32(r["Id"]),
                                    Descricao = r["Descricao"]?.ToString() ?? "",
                                    Categoria = r["Categoria"]?.ToString() ?? "",
                                    ValorReais = r["Valor"] == DBNull.Value ? "0" : Convert.ToDecimal(r["Valor"]).ToString("N2"),
                                    DataFormatada = r["DataVencimento"]?.ToString() ?? "",
                                    Status = r["Status"]?.ToString() ?? ""
                                };
                            }
                        }
                    }
                }
            }
            catch { }
            return null!;
        }

        public static void SalvarContaReceber(int id, int clienteId, string clienteNome, string descricao, string tipoDoc, decimal valor, string dataVencimento)
        {
            try
            {
                using (var cx = new SqliteConnection(ConnectionString))
                {
                    cx.Open();
                    if (id == 0)
                    {
                        string sql = "INSERT INTO ContasReceber (ClienteId, ClienteNome, Descricao, TipoDocumento, Valor, DataVencimento, Status) VALUES (@cid, @cnm, @d, @tdoc, @v, @dv, 'Pendente')";
                        using (var cmd = new SqliteCommand(sql, cx)) { cmd.Parameters.AddWithValue("@cid", clienteId); cmd.Parameters.AddWithValue("@cnm", clienteNome); cmd.Parameters.AddWithValue("@d", descricao); cmd.Parameters.AddWithValue("@tdoc", tipoDoc); cmd.Parameters.AddWithValue("@v", valor); cmd.Parameters.AddWithValue("@dv", dataVencimento); cmd.ExecuteNonQuery(); }
                    }
                    else
                    {
                        string sql = "UPDATE ContasReceber SET ClienteNome=@cnm, Descricao=@d, TipoDocumento=@tdoc, Valor=@v, DataVencimento=@dv WHERE Id=@id";
                        using (var cmd = new SqliteCommand(sql, cx)) { cmd.Parameters.AddWithValue("@cnm", clienteNome); cmd.Parameters.AddWithValue("@d", descricao); cmd.Parameters.AddWithValue("@tdoc", tipoDoc); cmd.Parameters.AddWithValue("@v", valor); cmd.Parameters.AddWithValue("@dv", dataVencimento); cmd.Parameters.AddWithValue("@id", id); cmd.ExecuteNonQuery(); }
                    }
                }
            }
            catch { }
        }

        public static List<ContasReceberModel> FiltrarContasReceber(int idClienteFiltro, string statusFiltro, out decimal totalPendente)
        {
            var lista = new List<ContasReceberModel>();
            totalPendente = 0;
            try
            {
                using (var cx = new SqliteConnection(ConnectionString))
                {
                    cx.Open();
                    string sql = "SELECT * FROM ContasReceber WHERE 1=1";
                    if (idClienteFiltro > 0) sql += " AND ClienteId = @cid";
                    if (statusFiltro != "Todos") sql += " AND Status = @status";
                    sql += " ORDER BY DataVencimento ASC";

                    using (var cmd = new SqliteCommand(sql, cx))
                    {
                        if (idClienteFiltro > 0) cmd.Parameters.AddWithValue("@cid", idClienteFiltro);
                        if (statusFiltro != "Todos") cmd.Parameters.AddWithValue("@status", statusFiltro);

                        using (var r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                decimal valor = r["Valor"] == DBNull.Value ? 0 : Convert.ToDecimal(r["Valor"]);
                                string status = r["Status"]?.ToString() ?? "";
                                if (status == "Pendente") totalPendente += valor;

                                string dataBanco = r["DataVencimento"]?.ToString() ?? "";
                                string dataTela = dataBanco;
                                if (DateTime.TryParse(dataBanco, out DateTime dt)) dataTela = dt.ToString("dd/MM/yyyy");

                                string tDoc = "Não Informado";
                                try { tDoc = r["TipoDocumento"]?.ToString() ?? "Carnê"; } catch { }

                                lista.Add(new ContasReceberModel
                                {
                                    Id = Convert.ToInt32(r["Id"]),
                                    ClienteNome = r["ClienteNome"]?.ToString() ?? "",
                                    Descricao = r["Descricao"]?.ToString() ?? "",
                                    TipoDocumento = tDoc,
                                    Valor = valor,
                                    ValorFormatado = valor.ToString("C"),
                                    DataVencimentoFormatada = dataTela,
                                    Status = status
                                });
                            }
                        }
                    }
                }
            }
            catch { }
            return lista;
        }

        public static ContasReceberModel ObterContaReceberPorId(int id)
        {
            try
            {
                using (var cx = new SqliteConnection(ConnectionString))
                {
                    cx.Open();
                    using (var cmd = new SqliteCommand("SELECT * FROM ContasReceber WHERE Id = @id", cx))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        using (var r = cmd.ExecuteReader())
                        {
                            if (r.Read())
                            {
                                string dataBanco = r["DataVencimento"]?.ToString() ?? "";
                                string tDoc = "Carnê/Promissória";
                                try { tDoc = r["TipoDocumento"]?.ToString() ?? "Carnê/Promissória"; } catch { }

                                return new ContasReceberModel
                                {
                                    Id = Convert.ToInt32(r["Id"]),
                                    ClienteNome = r["ClienteNome"]?.ToString() ?? "",
                                    Descricao = r["Descricao"]?.ToString() ?? "",
                                    TipoDocumento = tDoc,
                                    Valor = r["Valor"] == DBNull.Value ? 0 : Convert.ToDecimal(r["Valor"]),
                                    DataVencimentoFormatada = dataBanco
                                };
                            }
                        }
                    }
                }
            }
            catch { }
            return null!;
        }

        public static void ExcluirContaReceber(int id)
        {
            try { using (var cx = new SqliteConnection(ConnectionString)) { cx.Open(); using (var cmd = new SqliteCommand("DELETE FROM ContasReceber WHERE Id = @id", cx)) { cmd.Parameters.AddWithValue("@id", id); cmd.ExecuteNonQuery(); } } } catch { }
        }

        public static void BaixarContaReceber(int id, string formaPagamento, string dataPagamento)
        {
            try
            {
                using (var cx = new SqliteConnection(ConnectionString))
                {
                    cx.Open();
                    string sql = "UPDATE ContasReceber SET Status='Pago', DataPagamento=@data, FormaPagamento=@forma WHERE Id=@id";
                    using (var cmd = new SqliteCommand(sql, cx)) { cmd.Parameters.AddWithValue("@data", dataPagamento); cmd.Parameters.AddWithValue("@forma", formaPagamento); cmd.Parameters.AddWithValue("@id", id); cmd.ExecuteNonQuery(); }
                }
            }
            catch { }
        }

        public static void SalvarCliente(ClienteCompleto c)
        {
            try
            {
                using (var cx = new SqliteConnection(ConnectionString))
                {
                    cx.Open();
                    if (c.Id == 0)
                    {
                        string sql = @"INSERT INTO Clientes (Nome, CpfCnpj, Celular, TelefoneFixo, Tipo, Rg, Endereco, Numero, Complemento, Bairro, Cidade, Uf, Cep, Email, Observacoes, DataCadastro, CreditoLiberado) 
                                       VALUES (@nome, @cpf, @celular, @fixo, @tipo, @rg, @endereco, @numero, @complemento, @bairro, @cidade, @uf, @cep, @email, @obs, @data, @credito)";
                        using (var cmd = new SqliteCommand(sql, cx))
                        {
                            cmd.Parameters.AddWithValue("@nome", c.Nome ?? ""); cmd.Parameters.AddWithValue("@cpf", c.CpfCnpj ?? "");
                            cmd.Parameters.AddWithValue("@celular", c.Celular ?? ""); cmd.Parameters.AddWithValue("@fixo", c.TelefoneFixo ?? "");
                            cmd.Parameters.AddWithValue("@tipo", c.Tipo ?? "Física"); cmd.Parameters.AddWithValue("@rg", c.Rg ?? "");
                            cmd.Parameters.AddWithValue("@endereco", c.Endereco ?? ""); cmd.Parameters.AddWithValue("@numero", c.Numero ?? "SN");
                            cmd.Parameters.AddWithValue("@complemento", c.Complemento ?? ""); cmd.Parameters.AddWithValue("@bairro", c.Bairro ?? "");
                            cmd.Parameters.AddWithValue("@cidade", c.Cidade ?? ""); cmd.Parameters.AddWithValue("@uf", c.Uf ?? "SP");
                            cmd.Parameters.AddWithValue("@cep", c.Cep ?? ""); cmd.Parameters.AddWithValue("@email", c.Email ?? "");
                            cmd.Parameters.AddWithValue("@obs", c.Observacoes ?? ""); cmd.Parameters.AddWithValue("@data", c.DataCadastro ?? "");
                            cmd.Parameters.AddWithValue("@credito", c.CreditoLiberado);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        string sql = @"UPDATE Clientes SET Nome=@nome, CpfCnpj=@cpf, Celular=@celular, TelefoneFixo=@fixo, Tipo=@tipo, Rg=@rg, 
                                       Endereco=@endereco, Numero=@numero, Complemento=@complemento, Bairro=@bairro, Cidade=@cidade, Uf=@uf, 
                                       Cep=@cep, Email=@email, Observacoes=@obs, CreditoLiberado=@credito WHERE Id=@id";
                        using (var cmd = new SqliteCommand(sql, cx))
                        {
                            cmd.Parameters.AddWithValue("@nome", c.Nome ?? ""); cmd.Parameters.AddWithValue("@cpf", c.CpfCnpj ?? "");
                            cmd.Parameters.AddWithValue("@celular", c.Celular ?? ""); cmd.Parameters.AddWithValue("@fixo", c.TelefoneFixo ?? "");
                            cmd.Parameters.AddWithValue("@tipo", c.Tipo ?? "Física"); cmd.Parameters.AddWithValue("@rg", c.Rg ?? "");
                            cmd.Parameters.AddWithValue("@endereco", c.Endereco ?? ""); cmd.Parameters.AddWithValue("@numero", c.Numero ?? "SN");
                            cmd.Parameters.AddWithValue("@complemento", c.Complemento ?? ""); cmd.Parameters.AddWithValue("@bairro", c.Bairro ?? "");
                            cmd.Parameters.AddWithValue("@cidade", c.Cidade ?? ""); cmd.Parameters.AddWithValue("@uf", c.Uf ?? "SP");
                            cmd.Parameters.AddWithValue("@cep", c.Cep ?? ""); cmd.Parameters.AddWithValue("@email", c.Email ?? "");
                            cmd.Parameters.AddWithValue("@obs", c.Observacoes ?? ""); cmd.Parameters.AddWithValue("@id", c.Id);
                            cmd.Parameters.AddWithValue("@credito", c.CreditoLiberado);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Erro ao salvar cliente: " + ex.Message, "Erro BD", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        public static void ExcluirCliente(int id)
        {
            try
            {
                using (var cx = new SqliteConnection(ConnectionString))
                {
                    cx.Open();
                    using (var cmd = new SqliteCommand("DELETE FROM Clientes WHERE Id = @id", cx))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Erro ao excluir cliente: " + ex.Message, "Erro BD", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        public static ClienteCompleto ObterClientePorId(int id)
        {
            try
            {
                using (var cx = new SqliteConnection(ConnectionString))
                {
                    cx.Open();
                    using (var cmd = new SqliteCommand("SELECT * FROM Clientes WHERE Id = @id", cx))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        using (var r = cmd.ExecuteReader())
                        {
                            if (r.Read())
                            {
                                return new ClienteCompleto
                                {
                                    Id = Convert.ToInt32(r["Id"]),
                                    Nome = r["Nome"]?.ToString() ?? "",
                                    CpfCnpj = r["CpfCnpj"]?.ToString() ?? "",
                                    Rg = r["Rg"]?.ToString() ?? "",
                                    TelefoneFixo = r["TelefoneFixo"]?.ToString() ?? "",
                                    Celular = r["Celular"]?.ToString() ?? "",
                                    Email = r["Email"]?.ToString() ?? "",
                                    Cep = r["Cep"]?.ToString() ?? "",
                                    Endereco = r["Endereco"]?.ToString() ?? "",
                                    Numero = r["Numero"]?.ToString() ?? "",
                                    Complemento = r["Complemento"]?.ToString() ?? "",
                                    Bairro = r["Bairro"]?.ToString() ?? "",
                                    Cidade = r["Cidade"]?.ToString() ?? "",
                                    Uf = r["Uf"]?.ToString() ?? "",
                                    Tipo = r["Tipo"]?.ToString() ?? "",
                                    Observacoes = r["Observacoes"]?.ToString() ?? "",
                                    DataCadastro = r["DataCadastro"]?.ToString() ?? "",
                                    CreditoLiberado = r["CreditoLiberado"] == DBNull.Value ? 0 : Convert.ToDecimal(r["CreditoLiberado"])
                                };
                            }
                        }
                    }
                }
            }
            catch { }
            return null!;
        }

        public static List<HistoricoVendaProdutoModel> ObterHistoricoVendasProduto(string codigoProduto, out decimal somaTotal, out decimal qtdTotal)
        {
            var lista = new List<HistoricoVendaProdutoModel>();
            somaTotal = 0; qtdTotal = 0;
            try
            {
                using (var cx = new SqliteConnection(ConnectionString))
                {
                    cx.Open();
                    string sql = @"SELECT v.DataHora, v.NumeroVenda, v.ClienteNome, i.Quantidade, i.PrecoUnitario, i.Total 
                                   FROM ItensVenda i 
                                   INNER JOIN Vendas v ON i.VendaId = v.Id 
                                   WHERE i.CodigoProduto = @c 
                                   ORDER BY v.Id DESC";

                    using (var cmd = new SqliteCommand(sql, cx))
                    {
                        cmd.Parameters.AddWithValue("@c", codigoProduto);
                        using (var r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                decimal qtd = r["Quantidade"] == DBNull.Value ? 0 : Convert.ToDecimal(r["Quantidade"]);
                                decimal tot = r["Total"] == DBNull.Value ? 0 : Convert.ToDecimal(r["Total"]);
                                decimal un = r["PrecoUnitario"] == DBNull.Value ? 0 : Convert.ToDecimal(r["PrecoUnitario"]);

                                qtdTotal += qtd;
                                somaTotal += tot;

                                string dataBanco = r["DataHora"]?.ToString() ?? "";
                                if (DateTime.TryParse(dataBanco, out DateTime dt)) dataBanco = dt.ToString("dd/MM/yyyy HH:mm");

                                lista.Add(new HistoricoVendaProdutoModel
                                {
                                    DataVenda = dataBanco,
                                    NumVenda = r["NumeroVenda"]?.ToString() ?? "",
                                    Cliente = string.IsNullOrWhiteSpace(r["ClienteNome"]?.ToString()) ? "CONSUMIDOR FINAL" : r["ClienteNome"]?.ToString() ?? "",
                                    Quantidade = qtd.ToString("N2"),
                                    ValorUnitario = un.ToString("C"),
                                    Total = tot.ToString("C")
                                });
                            }
                        }
                    }
                }
            }
            catch { }
            return lista;
        }

        public static void RegistrarEntradaEstoque(string codigo, string fornecedor, string nfe, decimal qtd, decimal custoUn)
        {
            try
            {
                using (var cx = new SqliteConnection(ConnectionString))
                {
                    cx.Open();
                    string sqlHist = "INSERT INTO HistoricoCompras (CodigoProduto, DataCompra, Fornecedor, NumeroNFe, Quantidade, CustoUnitario, Total) VALUES (@c, @data, @f, @nfe, @qtd, @custo, @tot)";
                    using (var cmd = new SqliteCommand(sqlHist, cx))
                    {
                        cmd.Parameters.AddWithValue("@c", codigo);
                        cmd.Parameters.AddWithValue("@data", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                        cmd.Parameters.AddWithValue("@f", string.IsNullOrWhiteSpace(fornecedor) ? "Avulso" : fornecedor);
                        cmd.Parameters.AddWithValue("@nfe", string.IsNullOrWhiteSpace(nfe) ? "S/N" : nfe);
                        cmd.Parameters.AddWithValue("@qtd", qtd);
                        cmd.Parameters.AddWithValue("@custo", custoUn);
                        cmd.Parameters.AddWithValue("@tot", qtd * custoUn);
                        cmd.ExecuteNonQuery();
                    }

                    string sqlProd = "UPDATE Produtos SET EstoqueAtual = EstoqueAtual + @qtd, PrecoCompra = @custo WHERE Codigo = @c";
                    using (var cmd = new SqliteCommand(sqlProd, cx))
                    {
                        cmd.Parameters.AddWithValue("@qtd", qtd);
                        cmd.Parameters.AddWithValue("@custo", custoUn);
                        cmd.Parameters.AddWithValue("@c", codigo);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Erro ao registrar entrada: " + ex.Message); }
        }

        public static List<HistoricoCompraModel> ObterHistoricoComprasProduto(string codigoProduto)
        {
            var lista = new List<HistoricoCompraModel>();
            try
            {
                using (var cx = new SqliteConnection(ConnectionString))
                {
                    cx.Open();
                    string sql = "SELECT * FROM HistoricoCompras WHERE CodigoProduto = @c ORDER BY Id DESC";
                    using (var cmd = new SqliteCommand(sql, cx))
                    {
                        cmd.Parameters.AddWithValue("@c", codigoProduto);
                        using (var r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                decimal qtd = r["Quantidade"] == DBNull.Value ? 0 : Convert.ToDecimal(r["Quantidade"]);
                                decimal un = r["CustoUnitario"] == DBNull.Value ? 0 : Convert.ToDecimal(r["CustoUnitario"]);
                                decimal tot = r["Total"] == DBNull.Value ? 0 : Convert.ToDecimal(r["Total"]);

                                string dataBanco = r["DataCompra"]?.ToString() ?? "";
                                if (DateTime.TryParse(dataBanco, out DateTime dt)) dataBanco = dt.ToString("dd/MM/yyyy HH:mm");

                                lista.Add(new HistoricoCompraModel
                                {
                                    DataCompra = dataBanco,
                                    Fornecedor = r["Fornecedor"]?.ToString() ?? "",
                                    NumeroNFe = r["NumeroNFe"]?.ToString() ?? "",
                                    Quantidade = qtd.ToString("N2"),
                                    CustoUnitario = un.ToString("C"),
                                    Total = tot.ToString("C")
                                });
                            }
                        }
                    }
                }
            }
            catch { }
            return lista;
        }

        public static void RegistrarAjusteEstoque(string codigo, string tipo, decimal qtd, string motivo)
        {
            try
            {
                using (var cx = new SqliteConnection(ConnectionString))
                {
                    cx.Open();
                    string sqlHist = "INSERT INTO HistoricoEstoque (CodigoProduto, DataHora, Tipo, Quantidade, Motivo) VALUES (@c, @data, @t, @q, @m)";
                    using (var cmd = new SqliteCommand(sqlHist, cx))
                    {
                        cmd.Parameters.AddWithValue("@c", codigo);
                        cmd.Parameters.AddWithValue("@data", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                        cmd.Parameters.AddWithValue("@t", tipo);
                        cmd.Parameters.AddWithValue("@q", qtd);
                        cmd.Parameters.AddWithValue("@m", string.IsNullOrWhiteSpace(motivo) ? "Ajuste Manual" : motivo);
                        cmd.ExecuteNonQuery();
                    }

                    string sqlProd = "";
                    if (tipo == "Entrada Avulsa") sqlProd = "UPDATE Produtos SET EstoqueAtual = EstoqueAtual + @q WHERE Codigo = @c";
                    else if (tipo == "Saída (Perda/Avaria)") sqlProd = "UPDATE Produtos SET EstoqueAtual = EstoqueAtual - @q WHERE Codigo = @c";
                    else if (tipo == "Balanço (Substituir)") sqlProd = "UPDATE Produtos SET EstoqueAtual = @q WHERE Codigo = @c";

                    if (!string.IsNullOrEmpty(sqlProd))
                    {
                        using (var cmd = new SqliteCommand(sqlProd, cx))
                        {
                            cmd.Parameters.AddWithValue("@q", qtd);
                            cmd.Parameters.AddWithValue("@c", codigo);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Erro ao registrar ajuste de estoque: " + ex.Message); }
        }

        public static List<HistoricoEstoqueModel> ObterHistoricoEstoqueProduto(string codigoProduto)
        {
            var lista = new List<HistoricoEstoqueModel>();
            try
            {
                using (var cx = new SqliteConnection(ConnectionString))
                {
                    cx.Open();
                    string sql = "SELECT * FROM HistoricoEstoque WHERE CodigoProduto = @c ORDER BY Id DESC";
                    using (var cmd = new SqliteCommand(sql, cx))
                    {
                        cmd.Parameters.AddWithValue("@c", codigoProduto);
                        using (var r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                decimal qtd = r["Quantidade"] == DBNull.Value ? 0 : Convert.ToDecimal(r["Quantidade"]);
                                string dataBanco = r["DataHora"]?.ToString() ?? "";
                                if (DateTime.TryParse(dataBanco, out DateTime dt)) dataBanco = dt.ToString("dd/MM/yyyy HH:mm");

                                lista.Add(new HistoricoEstoqueModel
                                {
                                    DataHora = dataBanco,
                                    Tipo = r["Tipo"]?.ToString() ?? "",
                                    Quantidade = qtd.ToString("N2"),
                                    Motivo = r["Motivo"]?.ToString() ?? ""
                                });
                            }
                        }
                    }
                }
            }
            catch { }
            return lista;
        }

        // MÉTODOS VAZIOS PARA MANTER A COMPATIBILIDADE
        public static void InserirVenda(decimal t, string f) { }
        public static void InserirMovimentacaoCaixa(string t, decimal v, string m) { }
        public static decimal CalcularTotalVendasPorPagamento(string f, string d) { return 0; }
        public static decimal CalcularTotalMovimentacao(string t, string d) { return 0; }
        public static void LimparDadosDoTurno() { }
        public static long InserirOS(string c, string r, string d, string s, string p, string pr, string l, string o, decimal t) { return 0; }
        public static void SalvarLicenca(string c) { }
    }

    // --- MODELOS UNIFICADOS ---
    public class Produto { public string Codigo { get; set; } = ""; public string Descricao { get; set; } = ""; public decimal Preco { get; set; } }
    public class Cliente { public int Id { get; set; } public string Nome { get; set; } = ""; public string Cpf { get; set; } = ""; }
    public class ItemCarrinho { public string Codigo { get; set; } = ""; public string Descricao { get; set; } = ""; public string Unidade { get; set; } = "UN"; public decimal Quantidade { get; set; } public decimal PrecoUnitario { get; set; } public decimal TotalBruto => Quantidade * PrecoUnitario; public decimal TotalLiquido => Quantidade * PrecoUnitario; }
    public class ItemVenda { public int NumeroItem { get; set; } public string Codigo { get; set; } = ""; public string Descricao { get; set; } = ""; public int Quantidade { get; set; } public decimal ValorUnitario { get; set; } public decimal Subtotal => Quantidade * ValorUnitario; public string Ncm { get; set; } = ""; public string Cfop { get; set; } = ""; public string Csosn { get; set; } = ""; }
    public class ItemOS { public string Codigo { get; set; } = ""; public string Descricao { get; set; } = ""; public decimal Quantidade { get; set; } public decimal ValorUnitario { get; set; } public decimal TotalLiquido => Quantidade * ValorUnitario; public decimal DescontoPerc { get; set; } public decimal DescontoReal { get; set; } }
    public class ItemNFe { public string Descricao { get; set; } = ""; public string Ncm { get; set; } = ""; public decimal Quantidade { get; set; } public decimal ValorUnitario { get; set; } public decimal Total => Quantidade * ValorUnitario; }
    public class Configuracao { public string NomeLoja { get; set; } = ""; public string SenhaGerente { get; set; } = ""; public string TipoImpressora { get; set; } = "58mm"; public bool AceitaPix { get; set; } public string ChavePix { get; set; } = ""; public string Cnpj { get; set; } = ""; public string Telefone { get; set; } = ""; public string EnderecoCompleto { get; set; } = ""; public bool AceitaCredito { get; set; } public bool AceitaDebito { get; set; } public string ChaveLicenca { get; set; } = ""; public string CabecalhoCupom { get; set; } = ""; public string RodapeCupom { get; set; } = ""; }
    public class ClienteBusca { public int Id { get; set; } public string Nome { get; set; } = ""; public string Cpf { get; set; } = ""; public string Telefone { get; set; } = ""; }
    public class DespesaModel { public int Id { get; set; } public string Descricao { get; set; } = ""; public string Categoria { get; set; } = ""; public string ValorReais { get; set; } = ""; public string DataFormatada { get; set; } = ""; public string Status { get; set; } = ""; }
    public class ContasReceberModel { public int Id { get; set; } public string ClienteNome { get; set; } = ""; public string Descricao { get; set; } = ""; public decimal Valor { get; set; } public string Status { get; set; } = ""; public string ValorFormatado { get; set; } = ""; public string DataVencimentoFormatada { get; set; } = ""; public string TipoDocumento { get; set; } = ""; }
    public class RelatorioClienteModel { public string Codigo { get; set; } = ""; public string Nome { get; set; } = ""; public string CPF { get; set; } = ""; public string Celular { get; set; } = ""; public string Cidade { get; set; } = ""; public string UF { get; set; } = ""; }
    public class RelatorioProdutoModel { public string Codigo { get; set; } = ""; public string Descricao { get; set; } = ""; public string Unidade { get; set; } = ""; public string PrecoVenda { get; set; } = ""; public string Estoque { get; set; } = ""; }
    public class RelatorioVendaModel { public string NumVenda { get; set; } = ""; public string DataVenda { get; set; } = ""; public string FormaPagto { get; set; } = ""; public string ValorTotal { get; set; } = ""; }

    public class ClienteCompleto
    {
        public int Id { get; set; }
        public string Nome { get; set; } = "";
        public string CpfCnpj { get; set; } = "";
        public string Rg { get; set; } = "";
        public string Celular { get; set; } = "";
        public string TelefoneFixo { get; set; } = "";
        public string Email { get; set; } = "";
        public string Cep { get; set; } = "";
        public string Endereco { get; set; } = "";
        public string Numero { get; set; } = "";
        public string Complemento { get; set; } = "";
        public string Bairro { get; set; } = "";
        public string Cidade { get; set; } = "";
        public string Uf { get; set; } = "";
        public string Tipo { get; set; } = "";
        public string Observacoes { get; set; } = "";
        public string DataCadastro { get; set; } = "";
        public decimal CreditoLiberado { get; set; }
    }

    public class HistoricoVendaProdutoModel { public string DataVenda { get; set; } = ""; public string NumVenda { get; set; } = ""; public string Cliente { get; set; } = ""; public string Quantidade { get; set; } = ""; public string ValorUnitario { get; set; } = ""; public string Total { get; set; } = ""; }
    public class HistoricoCompraModel { public string DataCompra { get; set; } = ""; public string Fornecedor { get; set; } = ""; public string NumeroNFe { get; set; } = ""; public string Quantidade { get; set; } = ""; public string CustoUnitario { get; set; } = ""; public string Total { get; set; } = ""; }
    public class HistoricoEstoqueModel { public string DataHora { get; set; } = ""; public string Tipo { get; set; } = ""; public string Quantidade { get; set; } = ""; public string Motivo { get; set; } = ""; }
    public class ProdutoVariacaoModel { public int Id { get; set; } public string CodigoProduto { get; set; } = ""; public string Atributo { get; set; } = ""; public string Valor { get; set; } = ""; public string CodigoBarras { get; set; } = ""; public decimal Estoque { get; set; } }
}