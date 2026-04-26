using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using System.IO;
using System.Windows;

namespace SistemaCaixaPDV
{
    public static class BancoDeDados
    {
        // MUDAMOS PARA V3: Assim o Windows não bloqueia e o banco nasce com todas as colunas!
        public static readonly string CaminhoBanco = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bancopdv_v3.sqlite");
        public static string ConnectionString => $"Data Source={CaminhoBanco}";

        public static void InicializarBanco()
        {
            try
            {
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

                        @"CREATE TABLE IF NOT EXISTS ControleNFe (Id INTEGER PRIMARY KEY, Serie INTEGER, Numero INTEGER)"
                    };
                    foreach (var sql in comandos) { using (var cmd = new SqliteCommand(sql, conexao)) { cmd.ExecuteNonQuery(); } }
                }
            }
            catch { }
        }

        // ===============================================================
        // AGORA SIM: A LÓGICA DE VERDADE PARA SALVAR O PRODUTO
        // ===============================================================
        public static bool SalvarProdutoCompleto(string cod, string desc, string unid, decimal pCompra, decimal margem, decimal pVenda, decimal pPrazo, int estMin, string obs, string caminhoFotoAtual)
        {
            try
            {
                using (var cx = new SqliteConnection(ConnectionString))
                {
                    cx.Open();
                    // O comando INSERT OR REPLACE atualiza o produto se o código já existir, ou cria um novo!
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
                return true; // Sucesso!
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao salvar produto no banco: " + ex.Message, "Erro SQL", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        public static Configuracao ObterConfiguracoes()
        {
            var conf = new Configuracao { NomeLoja = "SISTEMA PDV", TipoImpressora = "58mm" };
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
                            conf.NomeLoja = r["NomeLoja"].ToString();
                            conf.SenhaGerente = r["SenhaGerente"].ToString();
                            conf.TipoImpressora = r["TipoImpressora"].ToString();
                            conf.AceitaPix = r["AceitaPix"].ToString() == "1";
                            conf.ChavePix = r["ChavePix"].ToString();
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
                    using (var r = cmd.ExecuteReader()) { while (r.Read()) { lista.Add(new Cliente { Id = Convert.ToInt32(r["Id"]), Nome = r["Nome"].ToString(), Cpf = r["CpfCnpj"].ToString() }); } }
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
                    using (var r = cmd.ExecuteReader()) { while (r.Read()) { lista.Add(r.GetString(0)); } }
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
                        using (var r = cmd.ExecuteReader()) { if (r.Read()) return new Produto { Codigo = cod, Descricao = r.GetString(0), Preco = r.GetDecimal(1) }; }
                    }
                }
            }
            catch { }
            return null;
        }

        public static List<Produto> BuscarProdutosPorNome(string n) { return new List<Produto>(); }
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
}