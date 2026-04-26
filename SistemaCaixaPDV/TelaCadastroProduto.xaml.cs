using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using Microsoft.Data.Sqlite;

namespace SistemaCaixaPDV
{
    public partial class TelaCadastroProduto : Window
    {
        private string caminhoFotoAtual = "";

        public TelaCadastroProduto()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LimparCampos();
        }

        // ==========================================
        // ATALHOS DE TECLADO
        // ==========================================
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F2) btnNovo_Click(sender, e);
            if (e.Key == Key.F3) btnGravar_Click(sender, e);
            if (e.Key == Key.F6) btnLocalizar_Click(sender, e);
            if (e.Key == Key.Escape) btnFechar_Click(sender, e);

            if (Keyboard.Modifiers == ModifierKeys.Alt && e.Key == Key.G)
            {
                btnGerarCodigo_Click(sender, e);
            }
        }

        // ==========================================
        // CÁLCULO DE PREÇO, MARGEM E ESTOQUE
        // ==========================================
        private void CalculoMargem_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txtPrecoCompra == null || txtMargem == null || txtLucro == null || txtPrecoVenda == null) return;
            try
            {
                decimal precoCompra = ConverterTextoParaDecimal(txtPrecoCompra.Text);
                decimal margemPercentual = ConverterTextoParaDecimal(txtMargem.Text);

                decimal lucroReais = (precoCompra * margemPercentual) / 100;
                decimal precoVenda = precoCompra + lucroReais;

                txtLucro.Text = lucroReais.ToString("N2");
                txtPrecoVenda.Text = precoVenda.ToString("N2");

                if (txtPrecoPrazo != null) txtPrecoPrazo.Text = precoVenda.ToString("N2");
            }
            catch { }
        }

        private decimal ConverterTextoParaDecimal(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return 0;
            texto = texto.Replace("R$", "").Trim();
            if (decimal.TryParse(texto, out decimal valor)) return valor;
            return 0;
        }

        private int LerEstoqueComoInteiro(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return 0;
            string parteInteira = texto.Split(',')[0].Split('.')[0].Trim();
            if (int.TryParse(parteInteira, out int valor)) return valor;
            return 0;
        }

        // ==========================================
        // AÇÕES DA BARRA SUPERIOR
        // ==========================================
        private void btnNovo_Click(object sender, RoutedEventArgs e)
        {
            LimparCampos();
        }

        private void btnGravar_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtCodigo.Text))
            {
                MessageBox.Show("O Produto precisa ter um Código!", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtCodigo.Focus(); return;
            }
            if (string.IsNullOrWhiteSpace(txtDescricao.Text))
            {
                MessageBox.Show("A Descrição é obrigatória!", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtDescricao.Focus(); return;
            }

            string cod = txtCodigo.Text.Trim();
            string desc = txtDescricao.Text.Trim().ToUpper();
            string unid = cbUnidade.Text;
            decimal pCompra = ConverterTextoParaDecimal(txtPrecoCompra.Text);
            decimal margem = ConverterTextoParaDecimal(txtMargem.Text);
            decimal pVenda = ConverterTextoParaDecimal(txtPrecoVenda.Text);
            decimal pPrazo = ConverterTextoParaDecimal(txtPrecoPrazo.Text);
            int estMin = LerEstoqueComoInteiro(txtEstoqueMinimo.Text);
            string obs = txtObservacoes.Text;

            // Salva via classe base
            object value = BancoDeDados.SalvarProdutoCompleto(cod, desc, unid, pCompra, margem, pVenda, pPrazo, estMin, obs, caminhoFotoAtual);

            // ==============================================================
            // FORÇA A ATUALIZAÇÃO DO ESTOQUE E DOS DADOS FISCAIS NO BANCO
            // ==============================================================
            try
            {
                int estAtual = LerEstoqueComoInteiro(txtEstoqueAtual.Text);
                string ncm = txtNcm.Text.Trim();
                string cfop = txtCfop.Text.Trim();
                string csosn = cbCsosn.Text;

                string connectionString = BancoDeDados.ConnectionString;
                using (var cx = new SqliteConnection(connectionString))
                {
                    cx.Open();

                    // Blindagem: Garante que as colunas fiscais existem
                    try { using (var cmdCol = new SqliteCommand("ALTER TABLE Produtos ADD COLUMN EstoqueAtual INTEGER", cx)) cmdCol.ExecuteNonQuery(); } catch { }
                    try { using (var cmdCol = new SqliteCommand("ALTER TABLE Produtos ADD COLUMN Ncm TEXT", cx)) cmdCol.ExecuteNonQuery(); } catch { }
                    try { using (var cmdCol = new SqliteCommand("ALTER TABLE Produtos ADD COLUMN Cfop TEXT", cx)) cmdCol.ExecuteNonQuery(); } catch { }
                    try { using (var cmdCol = new SqliteCommand("ALTER TABLE Produtos ADD COLUMN Csosn TEXT", cx)) cmdCol.ExecuteNonQuery(); } catch { }

                    string sqlUpdates = "UPDATE Produtos SET EstoqueAtual = @est, Ncm = @ncm, Cfop = @cfop, Csosn = @csosn WHERE Codigo = @c";
                    using (var cmd = new SqliteCommand(sqlUpdates, cx))
                    {
                        cmd.Parameters.AddWithValue("@est", estAtual);
                        cmd.Parameters.AddWithValue("@ncm", ncm);
                        cmd.Parameters.AddWithValue("@cfop", cfop);
                        cmd.Parameters.AddWithValue("@csosn", csosn);
                        cmd.Parameters.AddWithValue("@c", cod);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Atenção: Houve um erro ao registrar os dados extras (Fiscais/Estoque): " + ex.Message); }
            // ==============================================================

            MessageBox.Show("Produto salvo com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
            LimparCampos();
        }

        private void btnLocalizar_Click(object sender, RoutedEventArgs e)
        {
            TelaBuscaProduto telaBusca = new TelaBuscaProduto();
            telaBusca.Owner = this;

            if (telaBusca.ShowDialog() == true && telaBusca.ProdutoSelecionado != null)
            {
                string codigoEscolhido = telaBusca.ProdutoSelecionado.Codigo;
                string connectionString = BancoDeDados.ConnectionString;

                using (var cx = new SqliteConnection(connectionString))
                {
                    cx.Open();
                    string sql = "SELECT * FROM Produtos WHERE Codigo = @c";
                    using (var cmd = new SqliteCommand(sql, cx))
                    {
                        cmd.Parameters.AddWithValue("@c", codigoEscolhido);
                        using (var r = cmd.ExecuteReader())
                        {
                            if (r.Read())
                            {
                                txtCodigo.Text = r["Codigo"] == DBNull.Value ? "" : r["Codigo"].ToString();
                                txtDescricao.Text = r["Descricao"] == DBNull.Value ? "" : r["Descricao"].ToString();
                                cbUnidade.Text = r["Unidade"] == DBNull.Value ? "UN" : r["Unidade"].ToString();

                                txtPrecoCompra.Text = r["PrecoCompra"] == DBNull.Value ? "0,00" : Convert.ToDecimal(r["PrecoCompra"]).ToString("N2");
                                txtMargem.Text = r["Margem"] == DBNull.Value ? "0,00" : Convert.ToDecimal(r["Margem"]).ToString("N2");
                                txtPrecoVenda.Text = r["Preco"] == DBNull.Value ? "0,00" : Convert.ToDecimal(r["Preco"]).ToString("N2");
                                txtPrecoPrazo.Text = r["PrecoPrazo"] == DBNull.Value ? "0,00" : Convert.ToDecimal(r["PrecoPrazo"]).ToString("N2");

                                object estBD = r["EstoqueAtual"];
                                txtEstoqueAtual.Text = (estBD != DBNull.Value && estBD != null) ? Convert.ToInt32(Convert.ToDecimal(estBD)).ToString() : "0";

                                txtObservacoes.Text = r["Observacoes"] == DBNull.Value ? "" : r["Observacoes"].ToString();

                                // DADOS FISCAIS
                                txtNcm.Text = LerBancoSeguro(r, "Ncm");
                                txtCfop.Text = LerBancoSeguro(r, "Cfop");
                                string tributacaoBd = LerBancoSeguro(r, "Csosn");
                                cbCsosn.Text = string.IsNullOrEmpty(tributacaoBd) ? "102 - Tributada pelo Simples (Sem ST)" : tributacaoBd;

                                caminhoFotoAtual = r["CaminhoFoto"] == DBNull.Value ? "" : r["CaminhoFoto"].ToString();
                                if (!string.IsNullOrEmpty(caminhoFotoAtual))
                                {
                                    try { imgProduto.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(caminhoFotoAtual)); } catch { }
                                }
                            }
                        }
                    }
                }
            }
        }

        private string LerBancoSeguro(SqliteDataReader r, string coluna)
        {
            try { return r[coluna] == DBNull.Value ? "" : r[coluna].ToString(); } catch { return ""; }
        }

        private void btnExcluir_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtCodigo.Text)) { MessageBox.Show("Localize um produto primeiro!", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

            if (MessageBox.Show($"Excluir o produto:\n{txtDescricao.Text}?", "Excluir", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    using (var cx = new SqliteConnection(BancoDeDados.ConnectionString))
                    {
                        cx.Open();
                        using (var cmd = new SqliteCommand("DELETE FROM Produtos WHERE Codigo = @c", cx))
                        {
                            cmd.Parameters.AddWithValue("@c", txtCodigo.Text.Trim());
                            cmd.ExecuteNonQuery();
                        }
                    }
                    MessageBox.Show("Produto excluído!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                    LimparCampos();
                }
                catch (Exception ex) { MessageBox.Show("Erro: " + ex.Message); }
            }
        }

        private void btnFechar_Click(object sender, RoutedEventArgs e) { this.Close(); }

        // ==========================================
        // CÓDIGO E FOTO
        // ==========================================
        private void btnGerarCodigo_Click(object sender, RoutedEventArgs e)
        {
            txtCodigo.Text = DateTime.Now.ToString("yyMMddHHmmss");
            txtDescricao.Focus();
        }

        private void btnInserirFoto_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog { Filter = "Imagens|*.bmp;*.jpg;*.png" };
            if (dlg.ShowDialog() == true)
            {
                try { caminhoFotoAtual = dlg.FileName; imgProduto.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(caminhoFotoAtual)); }
                catch (Exception ex) { MessageBox.Show("Erro: " + ex.Message); }
            }
        }

        // ==========================================
        // LIMPEZA
        // ==========================================
        private void LimparCampos()
        {
            txtCodigo.Clear(); txtDescricao.Clear(); cbUnidade.Text = "UN";
            txtPrecoCompra.Text = "0,00"; txtMargem.Text = "100,00"; txtPrecoVenda.Text = "0,00"; txtPrecoPrazo.Text = "0,00";
            txtEstoqueMinimo.Text = "0"; txtEstoqueAtual.Text = "0"; txtObservacoes.Clear();

            // Limpa dados Fiscais com o Padrão do Restaurante
            txtNcm.Clear();
            txtCfop.Text = "5102";
            cbCsosn.Text = "102 - Tributada pelo Simples (Sem ST)";

            caminhoFotoAtual = "";
            if (imgProduto != null) imgProduto.Source = null;

            txtCodigo.Focus();
        }
    }
}
