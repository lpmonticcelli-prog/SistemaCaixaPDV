using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Data.Sqlite;

namespace SistemaCaixaPDV
{
    public partial class TelaEmissorNFe : Window
    {
        public ObservableCollection<ItemNFe> carrinhoDeProdutos { get; set; } = new ObservableCollection<ItemNFe>();

        public TelaEmissorNFe()
        {
            InitializeComponent();
            dgProdutosNFe.ItemsSource = carrinhoDeProdutos;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            CarregarNumeracaoNFe();
            CarregarClientes();
            CarregarProdutos();
        }

        // ===============================================================
        // BUSCAR CLIENTES NO BANCO DE DADOS
        // ===============================================================
        private void CarregarClientes()
        {
            try
            {
                using (var cx = new SqliteConnection(BancoDeDados.ConnectionString))
                {
                    cx.Open();
                    var cmd = new SqliteCommand("SELECT Id, Nome FROM Clientes ORDER BY Nome", cx);

                    using (var r = cmd.ExecuteReader())
                    {
                        cbBuscarCliente.Items.Clear();
                        int quantidadeClientes = 0;

                        while (r.Read())
                        {
                            cbBuscarCliente.Items.Add(new ComboBoxItem
                            {
                                Content = r["Nome"]?.ToString() ?? "",
                                Tag = r["Id"]?.ToString() ?? ""
                            });
                            quantidadeClientes++;
                        }

                        if (quantidadeClientes == 0)
                        {
                            MessageBox.Show("Aviso: A busca funcionou, mas não há nenhum cliente registrado no Banco de Dados!\nVá à tela de cadastro e crie um cliente de teste.", "Tabela Vazia", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("ERRO AO CARREGAR A LISTA DE CLIENTES: \n" + ex.Message + "\n\nVerifique se as colunas se chamam mesmo 'Id' e 'Nome'!", "Detetive de BD", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void cbBuscarCliente_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbBuscarCliente.SelectedItem is ComboBoxItem item && item.Tag != null)
            {
                string idCliente = item.Tag.ToString() ?? "";
                try
                {
                    using (var cx = new SqliteConnection(BancoDeDados.ConnectionString))
                    {
                        cx.Open();
                        var cmd = new SqliteCommand("SELECT * FROM Clientes WHERE Id = @id", cx);
                        cmd.Parameters.AddWithValue("@id", idCliente);

                        using (var r = cmd.ExecuteReader())
                        {
                            if (r.Read())
                            {
                                txtNomeCliente.Text = r["Nome"] == DBNull.Value ? "" : r["Nome"]?.ToString() ?? "";
                                txtCpfCnpjCliente.Text = r["CpfCnpj"] == DBNull.Value ? "" : r["CpfCnpj"]?.ToString() ?? "";
                                txtIeCliente.Text = r["Ie"] == DBNull.Value ? "" : r["Ie"]?.ToString() ?? "";
                                txtCepCliente.Text = r["Cep"] == DBNull.Value ? "" : r["Cep"]?.ToString() ?? "";
                                txtRuaCliente.Text = r["Rua"] == DBNull.Value ? "" : r["Rua"]?.ToString() ?? "";
                                txtNumeroCliente.Text = r["Numero"] == DBNull.Value ? "" : r["Numero"]?.ToString() ?? "";
                                txtBairroCliente.Text = r["Bairro"] == DBNull.Value ? "" : r["Bairro"]?.ToString() ?? "";
                                txtCidadeCliente.Text = r["Cidade"] == DBNull.Value ? "" : r["Cidade"]?.ToString() ?? "";
                                txtUfCliente.Text = r["Uf"] == DBNull.Value ? "" : r["Uf"]?.ToString() ?? "";

                                if (string.IsNullOrEmpty(txtIeCliente.Text) || txtCpfCnpjCliente.Text.Length <= 14)
                                    chkIsentoIE.IsChecked = true;
                                else
                                    chkIsentoIE.IsChecked = false;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("ERRO AO BUSCAR DADOS DO CLIENTE ESCOLHIDO: \n" + ex.Message, "Detetive de BD", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // ===============================================================
        // BUSCAR PRODUTOS NO BANCO DE DADOS
        // ===============================================================
        private void CarregarProdutos()
        {
            try
            {
                using (var cx = new SqliteConnection(BancoDeDados.ConnectionString))
                {
                    cx.Open();
                    var cmd = new SqliteCommand("SELECT Codigo, Descricao FROM Produtos ORDER BY Descricao", cx);

                    using (var r = cmd.ExecuteReader())
                    {
                        cbBuscarProduto.Items.Clear();
                        while (r.Read())
                        {
                            cbBuscarProduto.Items.Add(new ComboBoxItem
                            {
                                Content = r["Descricao"]?.ToString() ?? "",
                                Tag = r["Codigo"]?.ToString() ?? ""
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("ERRO AO CARREGAR A LISTA DE PRODUTOS: \n" + ex.Message, "Detetive de BD", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void cbBuscarProduto_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbBuscarProduto.SelectedItem is ComboBoxItem item && item.Tag != null)
            {
                string idProduto = item.Tag.ToString() ?? "";
                try
                {
                    using (var cx = new SqliteConnection(BancoDeDados.ConnectionString))
                    {
                        cx.Open();
                        var cmd = new SqliteCommand("SELECT * FROM Produtos WHERE Codigo = @id", cx);
                        cmd.Parameters.AddWithValue("@id", idProduto);

                        using (var r = cmd.ExecuteReader())
                        {
                            if (r.Read())
                            {
                                txtProduto.Text = r["Descricao"] == DBNull.Value ? "" : r["Descricao"]?.ToString() ?? "";
                                txtNcm.Text = r["Ncm"] == DBNull.Value ? "" : r["Ncm"]?.ToString() ?? "";
                                txtValor.Text = r["Preco"] == DBNull.Value ? "0,00" : r["Preco"]?.ToString() ?? "0,00";
                                txtQtd.Text = "1";

                                CalcularTotalProdutoLogica();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("ERRO AO BUSCAR DADOS DO PRODUTO ESCOLHIDO: \n" + ex.Message, "Detetive de BD", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // ===============================================================
        // MÁGICA DA NUMERAÇÃO AUTOMÁTICA
        // ===============================================================
        private void CarregarNumeracaoNFe()
        {
            try
            {
                using (var cx = new SqliteConnection(BancoDeDados.ConnectionString))
                {
                    cx.Open();
                    new SqliteCommand("CREATE TABLE IF NOT EXISTS ControleNFe (Id INTEGER PRIMARY KEY, Serie INTEGER, Numero INTEGER)", cx).ExecuteNonQuery();

                    var cmdCheck = new SqliteCommand("SELECT COUNT(*) FROM ControleNFe", cx);
                    if (Convert.ToInt64(cmdCheck.ExecuteScalar() ?? 0) == 0)
                    {
                        new SqliteCommand("INSERT INTO ControleNFe (Serie, Numero) VALUES (1, 1)", cx).ExecuteNonQuery();
                    }

                    using (var r = new SqliteCommand("SELECT Serie, Numero FROM ControleNFe LIMIT 1", cx).ExecuteReader())
                    {
                        if (r.Read())
                        {
                            txtSerieNFe.Text = r["Serie"]?.ToString() ?? "1";
                            txtNumeroNFe.Text = r["Numero"]?.ToString() ?? "1";
                        }
                    }
                }
            }
            catch { }
        }

        private void AtualizarNumeracaoParaProxima()
        {
            try
            {
                int proximoNumero = int.Parse(txtNumeroNFe.Text) + 1;
                txtNumeroNFe.Text = proximoNumero.ToString();

                using (var cx = new SqliteConnection(BancoDeDados.ConnectionString))
                {
                    cx.Open();
                    var cmd = new SqliteCommand("UPDATE ControleNFe SET Numero = @num, Serie = @serie", cx);
                    cmd.Parameters.AddWithValue("@num", proximoNumero);
                    cmd.Parameters.AddWithValue("@serie", int.Parse(txtSerieNFe.Text));
                    cmd.ExecuteNonQuery();
                }
            }
            catch { }
        }

        // ===============================================================
        // CONTROLE DA CAIXA "ISENTO" DA INSCRIÇÃO ESTADUAL
        // ===============================================================
        private void chkIsentoIE_Checked(object sender, RoutedEventArgs e)
        {
            if (txtIeCliente != null)
            {
                txtIeCliente.Text = "ISENTO";
                txtIeCliente.IsReadOnly = true;
                txtIeCliente.Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#F9FAFB"));
            }
        }

        private void chkIsentoIE_Unchecked(object sender, RoutedEventArgs e)
        {
            if (txtIeCliente != null)
            {
                txtIeCliente.Text = "";
                txtIeCliente.IsReadOnly = false;
                txtIeCliente.Background = System.Windows.Media.Brushes.White;
            }
        }

        // ===============================================================
        // LÓGICA DO CARRINHO DE COMPRAS
        // ===============================================================
        private void CalcularTotalProduto(object sender, TextChangedEventArgs e)
        {
            CalcularTotalProdutoLogica();
        }

        private void CalcularTotalProdutoLogica()
        {
            if (txtQtd == null || txtValor == null || txtTotalItem == null) return;

            if (decimal.TryParse(txtQtd.Text, out decimal qtd) && decimal.TryParse(txtValor.Text, out decimal valor))
            {
                txtTotalItem.Text = (qtd * valor).ToString("C");
            }
        }

        private void btnAdicionarProduto_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtProduto.Text) || string.IsNullOrEmpty(txtNcm.Text))
            {
                MessageBox.Show("Por favor, preencha a descrição e o NCM do produto.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (decimal.TryParse(txtQtd.Text, out decimal qtd) && decimal.TryParse(txtValor.Text, out decimal valor))
            {
                carrinhoDeProdutos.Add(new ItemNFe { Descricao = txtProduto.Text, Ncm = txtNcm.Text, Quantidade = qtd, ValorUnitario = valor });

                AtualizarTotalNFe();
            }
            else
            {
                MessageBox.Show("Verifique os valores de Quantidade e Valor Unitário.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnRemoverProduto_Click(object sender, RoutedEventArgs e)
        {
            if (dgProdutosNFe.SelectedItem is ItemNFe item)
            {
                carrinhoDeProdutos.Remove(item);
                AtualizarTotalNFe();
            }
            else
            {
                MessageBox.Show("Selecione um produto na grelha para remover.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void AtualizarTotalNFe()
        {
            decimal total = carrinhoDeProdutos.Sum(p => p.Total);
            txtTotalNFe.Text = total.ToString("C");
        }

        // ===============================================================
        // BOTÃO FINAL DE EMISSÃO COM A CHAMADA DO MOTOR
        // ===============================================================
        private void btnEmitirNFe_Click(object sender, RoutedEventArgs e)
        {
            if (carrinhoDeProdutos.Count == 0)
            {
                MessageBox.Show("Adicione pelo menos um produto na nota!", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(txtCpfCnpjCliente.Text))
            {
                MessageBox.Show("Preencha o CPF ou CNPJ do cliente!", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            this.Cursor = System.Windows.Input.Cursors.Wait;

            bool sucesso = MotorFiscalNFe.EmitirNFeModelo55(
                txtSerieNFe.Text,
                int.Parse(txtNumeroNFe.Text),
                txtCpfCnpjCliente.Text,
                txtNomeCliente.Text,
                txtCepCliente.Text,
                txtRuaCliente.Text,
                txtNumeroCliente.Text,
                txtBairroCliente.Text,
                txtCidadeCliente.Text,
                txtUfCliente.Text,
                txtIeCliente.Text,
                chkIsentoIE.IsChecked == true,
                carrinhoDeProdutos.ToList()
            );

            this.Cursor = System.Windows.Input.Cursors.Arrow;

            if (sucesso)
            {
                AtualizarNumeracaoParaProxima();
                carrinhoDeProdutos.Clear();
                AtualizarTotalNFe();
            }
        }
    }
}