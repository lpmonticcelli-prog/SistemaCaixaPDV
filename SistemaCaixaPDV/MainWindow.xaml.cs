using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Data.Sqlite;

namespace SistemaCaixaPDV
{
    public partial class MainWindow : Window
    {
        public ObservableCollection<ItemVenda> ItensDaVenda { get; set; }
        private decimal TotalVenda = 0;
        private decimal ValorDesconto = 0;
        private int ContadorItens = 1;
        private int QuantidadeAtual = 1;
        private DispatcherTimer timer;

        private string connectionString = BancoDeDados.ConnectionString;

        public MainWindow()
        {
            InitializeComponent();
            ItensDaVenda = new ObservableCollection<ItemVenda>();
            gridItens.ItemsSource = ItensDaVenda;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txtDataVenda.Text = DateTime.Now.ToString("dd/MM/yyyy");

            CarregarDadosDoCaixa();
            CarregarPropaganda();

            txtCodigoBarras.Focus();
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            txtRelogio.Text = DateTime.Now.ToString("HH:mm:ss");
        }

        private void CarregarDadosDoCaixa()
        {
            int proximaVenda = BancoDeDados.ObterProximoNumeroVenda();
            txtNumeroVenda.Text = $"#{proximaVenda:D5}";
            cbCliente.ItemsSource = BancoDeDados.ListarNomesClientes();
            cbCliente.Text = "CONSUMIDOR FINAL";
        }

        // ==========================================
        // MARKETING E FOTOS
        // ==========================================
        private void CarregarPropaganda()
        {
            try
            {
                using (var cx = new SqliteConnection(connectionString))
                {
                    cx.Open();
                    using (var cmd = new SqliteCommand("SELECT CaminhoBanner FROM Configuracoes LIMIT 1", cx))
                    {
                        var res = cmd.ExecuteScalar();
                        if (res != null && res != DBNull.Value)
                        {
                            string caminhoBannerBD = res.ToString();
                            if (File.Exists(caminhoBannerBD))
                            {
                                imgPropaganda.Source = new BitmapImage(new Uri(caminhoBannerBD));
                            }
                        }
                    }
                }
            }
            catch { }
        }

        private void ExibirFotoProduto(string caminhoDaFoto)
        {
            try
            {
                if (!string.IsNullOrEmpty(caminhoDaFoto) && File.Exists(caminhoDaFoto))
                    imgProdutoAtual.Source = new BitmapImage(new Uri(caminhoDaFoto));
                else
                    imgProdutoAtual.Source = null;
            }
            catch { imgProdutoAtual.Source = null; }
        }

        // ==========================================
        // ATALHOS DE TECLADO E GRID
        // ==========================================
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) this.Close();
            else if (e.Key == Key.F2) LimparCaixa(true);
            else if (e.Key == Key.F5 || (Keyboard.Modifiers == ModifierKeys.Alt && e.Key == Key.F)) btnDinheiro_Click(null, null);
            else if (e.Key == Key.F6 || e.Key == Key.Delete) { e.Handled = true; ExcluirItemSelecionado(); }
            else if (e.Key == Key.F4) AlterarQuantidadeSelecionado();
            else if (e.Key == Key.F11) AplicarDesconto();
            else if (e.Key == Key.F12) AbrirSangria();
            else if (e.Key == Key.F3) AbrirBuscaProduto();
            else if (e.Key == Key.D && string.IsNullOrWhiteSpace(txtCodigoBarras.Text)) { AbrirBuscaProduto(); e.Handled = true; }
            else if (e.Key == Key.Q) { QuantidadeAtual++; txtQuantidadeTela.Text = QuantidadeAtual.ToString(); txtCodigoBarras.Focus(); e.Handled = true; }
        }

        private void gridItens_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete) { e.Handled = true; ExcluirItemSelecionado(); }
        }

        private void gridItens_SelectionChanged(object sender, SelectionChangedEventArgs e) { }

        // ==========================================
        // FUNÇÕES DO CAIXA
        // ==========================================
        private bool VerificarSenhaGerente()
        {
            TelaSenhaGerente tela = new TelaSenhaGerente();
            tela.Owner = this;
            tela.ShowDialog();
            return tela.Autorizado;
        }

        private void AbrirSangria()
        {
            if (VerificarSenhaGerente())
            {
                TelaSangria tela = new TelaSangria();
                tela.Owner = this;
                tela.ShowDialog();
            }
            txtCodigoBarras.Focus();
        }

        private void AplicarDesconto()
        {
            if (ItensDaVenda.Count == 0) { MessageBox.Show("Passe algum produto no caixa antes de aplicar o desconto!", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information); return; }
            decimal subtotalBruto = 0; foreach (var item in ItensDaVenda) { subtotalBruto += item.Subtotal; }
            TelaDesconto telaDesc = new TelaDesconto(subtotalBruto); telaDesc.Owner = this;
            if (telaDesc.ShowDialog() == true) { if (VerificarSenhaGerente()) { ValorDesconto = telaDesc.ValorDescontoReais; RecalcularTotais(); } }
            txtCodigoBarras.Focus();
        }

        private void ExcluirItemSelecionado()
        {
            if (gridItens.SelectedItem != null)
            {
                if (!VerificarSenhaGerente()) return;
                ItemVenda itemSelecionado = (ItemVenda)gridItens.SelectedItem;
                if (MessageBox.Show($"Deseja excluir o item [{itemSelecionado.Descricao}]?", "Excluir", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    ItensDaVenda.Remove(itemSelecionado); ReorganizarNumeracao();
                    if (ItensDaVenda.Count == 0) ValorDesconto = 0;
                    RecalcularTotais(); txtCodigoBarras.Focus();
                }
            }
            else MessageBox.Show("Selecione um item na lista para excluir!", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void AlterarQuantidadeSelecionado()
        {
            if (gridItens.SelectedItem != null)
            {
                if (!VerificarSenhaGerente()) return;
                ItemVenda itemSelecionado = (ItemVenda)gridItens.SelectedItem;
                itemSelecionado.Quantidade = QuantidadeAtual;
                gridItens.Items.Refresh(); RecalcularTotais();
                QuantidadeAtual = 1; txtQuantidadeTela.Text = "1"; txtCodigoBarras.Focus();
            }
            else MessageBox.Show("Para alterar:\n1. Aperte (Q) até a nova quantidade.\n2. Clique no item da lista.\n3. Aperte F4.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ReorganizarNumeracao()
        {
            ContadorItens = 1;
            foreach (var item in ItensDaVenda) { item.NumeroItem = ContadorItens++; }
            gridItens.Items.Refresh();
        }

        private void RecalcularTotais()
        {
            TotalVenda = 0; foreach (var item in ItensDaVenda) { TotalVenda += item.Subtotal; }
            TotalVenda -= ValorDesconto; if (TotalVenda < 0) TotalVenda = 0;
            txtTotalVenda.Text = TotalVenda.ToString("C"); txtDescontoVisual.Text = ValorDesconto.ToString("C"); txtQtdTotalItens.Text = ItensDaVenda.Count.ToString();
        }

        private void btnNovaVenda_Click(object sender, RoutedEventArgs e) { LimparCaixa(true); }

        private void LimparCaixa(bool pedirConfirmacao)
        {
            if (pedirConfirmacao && ItensDaVenda.Count > 0) { if (MessageBox.Show("Cancelar a venda atual?", "Cancelar", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No) { txtCodigoBarras.Focus(); return; } }
            ItensDaVenda.Clear(); TotalVenda = 0; ValorDesconto = 0; ContadorItens = 1; QuantidadeAtual = 1;
            txtTotalVenda.Text = "R$ 0,00"; txtDescontoVisual.Text = "R$ 0,00"; txtDescricaoTela.Text = "Aguardando..."; txtValorUnitarioTela.Text = "R$ 0,00"; txtQuantidadeTela.Text = "1"; txtQtdTotalItens.Text = "0";
            imgProdutoAtual.Source = null;
            CarregarDadosDoCaixa();
            txtCodigoBarras.Clear(); txtCodigoBarras.Focus();
        }

        // ==========================================
        // BIPANDO O PRODUTO E BUSCA FISCAL/FOTO
        // ==========================================
        private void txtCodigoBarras_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                string codigoDigitado = txtCodigoBarras.Text.Trim();
                Produto produtoEncontrado = BancoDeDados.BuscarProduto(codigoDigitado);

                if (produtoEncontrado != null)
                {
                    AdicionarItemNaTela(produtoEncontrado);
                }
                else
                {
                    MessageBox.Show("Produto não encontrado no sistema!", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                txtCodigoBarras.Clear(); txtCodigoBarras.Focus();
            }
        }

        private void AdicionarItemNaTela(Produto prod)
        {
            string caminhoFotoBD = "";
            string ncmBD = "";
            string cfopBD = "";
            string csosnBD = "";

            // Busca a Foto e os Dados Fiscais direto no SQLite
            try
            {
                using (var cx = new SqliteConnection(connectionString))
                {
                    cx.Open();
                    using (var cmd = new SqliteCommand("SELECT CaminhoFoto, Ncm, Cfop, Csosn FROM Produtos WHERE Codigo = @c", cx))
                    {
                        cmd.Parameters.AddWithValue("@c", prod.Codigo);
                        using (var r = cmd.ExecuteReader())
                        {
                            if (r.Read())
                            {
                                caminhoFotoBD = r["CaminhoFoto"] == DBNull.Value ? "" : r["CaminhoFoto"].ToString();
                                ncmBD = r["Ncm"] == DBNull.Value ? "" : r["Ncm"].ToString();
                                cfopBD = r["Cfop"] == DBNull.Value ? "5102" : r["Cfop"].ToString();
                                csosnBD = r["Csosn"] == DBNull.Value ? "102" : r["Csosn"].ToString();
                            }
                        }
                    }
                }
            }
            catch { }

            var novoItem = new ItemVenda
            {
                NumeroItem = ContadorItens++,
                Codigo = prod.Codigo,
                Descricao = prod.Descricao,
                Quantidade = QuantidadeAtual,
                ValorUnitario = prod.Preco,
                Ncm = ncmBD,
                Cfop = cfopBD,
                Csosn = csosnBD
            };

            ItensDaVenda.Add(novoItem);
            RecalcularTotais();

            txtDescricaoTela.Text = prod.Descricao;
            txtValorUnitarioTela.Text = prod.Preco.ToString("C");
            ExibirFotoProduto(caminhoFotoBD);

            gridItens.ScrollIntoView(novoItem);
            QuantidadeAtual = 1;
            txtQuantidadeTela.Text = "1";
        }

        private void AbrirBuscaProduto()
        {
            TelaBuscaProduto telaBusca = new TelaBuscaProduto(); telaBusca.Owner = this;
            if (telaBusca.ShowDialog() == true && telaBusca.ProdutoSelecionado != null) { AdicionarItemNaTela(telaBusca.ProdutoSelecionado); }
            txtCodigoBarras.Focus();
        }

        // ==========================================
        // FINALIZAÇÃO DE VENDA E INTEGRAÇÃO FISCAL
        // ==========================================
        private void btnDinheiro_Click(object sender, RoutedEventArgs e)
        {
            if (TotalVenda > 0)
            {
                TelaPagamento telaPagamento = new TelaPagamento(TotalVenda);
                telaPagamento.Owner = this;

                if (telaPagamento.ShowDialog() == true)
                {
                    BancoDeDados.InserirVenda(TotalVenda, telaPagamento.FormaPagamentoSelecionada);

                    // BAIXA DE ESTOQUE
                    try
                    {
                        using (var cx = new SqliteConnection(connectionString))
                        {
                            cx.Open();
                            foreach (var item in ItensDaVenda)
                            {
                                decimal estoqueAtualNoBanco = 0;
                                using (var cmdBusca = new SqliteCommand("SELECT EstoqueAtual FROM Produtos WHERE Codigo = @cod", cx))
                                {
                                    cmdBusca.Parameters.AddWithValue("@cod", item.Codigo);
                                    var res = cmdBusca.ExecuteScalar();
                                    if (res != null && res != DBNull.Value) decimal.TryParse(res.ToString(), out estoqueAtualNoBanco);
                                }

                                decimal estoqueNovo = estoqueAtualNoBanco - item.Quantidade;
                                using (var cmdAtualiza = new SqliteCommand("UPDATE Produtos SET EstoqueAtual = @estNovo WHERE Codigo = @cod", cx))
                                {
                                    cmdAtualiza.Parameters.AddWithValue("@estNovo", estoqueNovo);
                                    cmdAtualiza.Parameters.AddWithValue("@cod", item.Codigo);
                                    cmdAtualiza.ExecuteNonQuery();
                                }
                            }
                        }
                    }
                    catch { }

                    decimal subtotalBruto = TotalVenda + ValorDesconto;

                    // ==============================================================
                    // MÁGICA FISCAL "NÍVEL GRANDE EMPRESA" (LISTA COMPLETA)
                    // ==============================================================
                    int numeroVenda = 1;
                    int.TryParse(txtNumeroVenda.Text.Replace("#", ""), out numeroVenda);

                    if (ItensDaVenda.Count > 0)
                    {
                        MotorFiscalNFCe.EmitirNFCeProducao(
                            "1",                            // Série
                            numeroVenda,                    // Número da nota
                            "",                             // CPF (vazio por agora)
                            cbCliente.Text,                 // Nome do Cliente
                            new List<ItemVenda>(ItensDaVenda), // A LISTA INTEIRA DE PRODUTOS!
                            ValorDesconto,                  // O Desconto da Venda
                            telaPagamento.FormaPagamentoSelecionada // O MEIO DE PAGAMENTO!
                        );
                    }
                    // ==============================================================

                    ImpressorCupom.Imprimir(new List<ItemVenda>(ItensDaVenda), subtotalBruto, ValorDesconto, TotalVenda, telaPagamento.FormaPagamentoSelecionada, cbCliente.Text, "");

                    LimparCaixa(false);
                }
            }
            else { MessageBox.Show("Não há itens na venda!", "Aviso", MessageBoxButton.OK, MessageBoxImage.Asterisk); txtCodigoBarras.Focus(); }
        }

        private void btnTelaCompleta_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("O Módulo de Vendas Complexas será desenvolvido em breve!", "Em breve", MessageBoxButton.OK, MessageBoxImage.Information);
            txtCodigoBarras.Focus();
        }

        private void btnSair_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
