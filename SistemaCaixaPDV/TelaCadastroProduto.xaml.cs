using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using Microsoft.Data.Sqlite; // DRIVER CORRETO INJETADO
using System.IO;
using System.Collections.ObjectModel;
using System.Collections.Generic;

namespace SistemaCaixaPDV
{
    public partial class TelaCadastroProduto : Window
    {
        private string caminhoFotoAtual = "";

        // Lista dinâmica ligada à aba 5 que usa o novo modelo do BancoDeDados
        public ObservableCollection<ProdutoVariacaoModel> ListaVariacoes { get; set; } = new ObservableCollection<ProdutoVariacaoModel>();

        public TelaCadastroProduto()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (gridVariacoes != null)
                gridVariacoes.ItemsSource = ListaVariacoes;

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
        // AÇÕES DA BARRA SUPERIOR E SALVAMENTO
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

            object value = BancoDeDados.SalvarProdutoCompleto(cod, desc, unid, pCompra, margem, pVenda, pPrazo, estMin, obs, caminhoFotoAtual);

            try
            {
                int estAtual = LerEstoqueComoInteiro(txtEstoqueAtual.Text);
                string ncm = txtNcm.Text.Trim();
                string cfop = txtCfop.Text.Trim();
                string csosn = cbCsosn.Text;

                using (var cx = new SqliteConnection(BancoDeDados.ConnectionString))
                {
                    cx.Open();

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

                // SALVA A GRADE / VARIAÇÕES (Aba 5)
                BancoDeDados.SalvarVariacoesProduto(cod, new List<ProdutoVariacaoModel>(ListaVariacoes));
            }
            catch (Exception ex) { MessageBox.Show("Atenção: Houve um erro ao registrar os dados extras: " + ex.Message); }

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

                using (var cx = new SqliteConnection(BancoDeDados.ConnectionString))
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

                                txtNcm.Text = LerBancoSeguro(r, "Ncm");
                                txtCfop.Text = LerBancoSeguro(r, "Cfop");
                                string tributacaoBd = LerBancoSeguro(r, "Csosn");
                                cbCsosn.Text = string.IsNullOrEmpty(tributacaoBd) ? "102 - Tributada pelo Simples (Sem ST)" : tributacaoBd;

                                caminhoFotoAtual = r["CaminhoFoto"] == DBNull.Value ? "" : r["CaminhoFoto"].ToString();
                                if (!string.IsNullOrEmpty(caminhoFotoAtual))
                                {
                                    try
                                    {
                                        string caminhoAbsoluto = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, caminhoFotoAtual);
                                        if (File.Exists(caminhoAbsoluto)) imgProduto.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(caminhoAbsoluto));
                                        else if (File.Exists(caminhoFotoAtual)) imgProduto.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(caminhoFotoAtual));
                                        else imgProduto.Source = null;
                                    }
                                    catch { imgProduto.Source = null; }
                                }
                                else { imgProduto.Source = null; }

                                CarregarHistoricoVendas(codigoEscolhido);
                                CarregarHistoricoCompras(codigoEscolhido);
                                CarregarHistoricoEstoque(codigoEscolhido);

                                // CARREGA A GRADE / VARIAÇÕES (Aba 5)
                                ListaVariacoes.Clear();
                                var varsDoBanco = BancoDeDados.ObterVariacoesProduto(codigoEscolhido);
                                foreach (var v in varsDoBanco) { ListaVariacoes.Add(v); }
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

                        // Exclui também as variações atreladas
                        using (var cmd = new SqliteCommand("DELETE FROM ProdutoVariacoes WHERE CodigoProduto = @c", cx))
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

        // ==========================================================
        // IMPRESSÃO E RELATÓRIOS
        // ==========================================================
        private void btnImprimirEtiqueta_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtCodigo.Text) || string.IsNullOrWhiteSpace(txtDescricao.Text))
            {
                MessageBox.Show("Localize ou cadastre um produto primeiro para imprimir a etiqueta.", "Atenção", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Abre a janela de etiqueta passando os dados do produto atual
                // Obs: Requer que a JanelaEtiqueta esteja implementada corretamente
                JanelaEtiqueta telaEtiqueta = new JanelaEtiqueta(txtDescricao.Text, txtPrecoVenda.Text, txtCodigo.Text);
                telaEtiqueta.Owner = this;
                telaEtiqueta.ShowDialog();
            }
            catch { MessageBox.Show("Módulo de JanelaEtiqueta não encontrado ou com erro de compilação."); }
        }

        private void btnImprimirRelatorio_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Abre a janela do relatório geral de produtos
                JanelaRelatorio telaRelatorio = new JanelaRelatorio();
                telaRelatorio.Owner = this;
                telaRelatorio.ShowDialog();
            }
            catch { MessageBox.Show("Módulo de JanelaRelatorio não encontrado ou com erro de compilação."); }
        }

        private void btnFechar_Click(object sender, RoutedEventArgs e) { this.Close(); }

        // ==========================================================
        // CÓDIGO E FOTO (COFRE)
        // ==========================================================
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
                try
                {
                    string pastaSegura = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Imagens", "Produtos");
                    if (!Directory.Exists(pastaSegura)) Directory.CreateDirectory(pastaSegura);

                    string extensao = Path.GetExtension(dlg.FileName);
                    string nomeArquivoSeguro = Guid.NewGuid().ToString() + extensao;
                    string caminhoDestinoFinal = Path.Combine(pastaSegura, nomeArquivoSeguro);

                    File.Copy(dlg.FileName, caminhoDestinoFinal, true);
                    caminhoFotoAtual = Path.Combine("Imagens", "Produtos", nomeArquivoSeguro);
                    imgProduto.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(caminhoDestinoFinal));
                }
                catch (Exception ex) { MessageBox.Show("Erro ao arquivar a foto: " + ex.Message, "Erro", MessageBoxButton.OK, MessageBoxImage.Error); }
            }
        }

        // ==========================================================
        // CARREGAMENTO DE ABAS E BOTÕES DE AÇÃO
        // ==========================================================
        private void CarregarHistoricoVendas(string codigoProduto)
        {
            if (gridHistoricoVendas == null) return;
            decimal somaTotal, qtdTotal;
            gridHistoricoVendas.ItemsSource = BancoDeDados.ObterHistoricoVendasProduto(codigoProduto, out somaTotal, out qtdTotal);
            if (txtTotalValorVenda != null) txtTotalValorVenda.Text = somaTotal.ToString("C");
            if (txtTotalQtdVenda != null) txtTotalQtdVenda.Text = qtdTotal.ToString("N2");
        }

        private void CarregarHistoricoCompras(string codigoProduto)
        {
            if (gridHistoricoCompras != null)
                gridHistoricoCompras.ItemsSource = BancoDeDados.ObterHistoricoComprasProduto(codigoProduto);
        }

        private void CarregarHistoricoEstoque(string codigoProduto)
        {
            if (gridHistoricoEstoque != null)
                gridHistoricoEstoque.ItemsSource = BancoDeDados.ObterHistoricoEstoqueProduto(codigoProduto);
        }

        private void btnLancarEntradaManual_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtCodigo.Text))
            {
                MessageBox.Show("Localize ou grave um produto primeiro antes de dar entrada no estoque!", "Atenção", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            decimal qtd = ConverterTextoParaDecimal(txtEntradaQtd.Text);
            decimal custo = ConverterTextoParaDecimal(txtEntradaCusto.Text);

            if (qtd <= 0)
            {
                MessageBox.Show("A quantidade de entrada deve ser maior que zero.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            BancoDeDados.RegistrarEntradaEstoque(txtCodigo.Text, txtEntradaFornecedor.Text, txtEntradaNFe.Text, qtd, custo);
            MessageBox.Show("Entrada registrada com sucesso! O estoque e o preço de custo foram atualizados.", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);

            txtEntradaFornecedor.Clear();
            txtEntradaNFe.Clear();
            txtEntradaQtd.Clear();
            txtEntradaCusto.Clear();

            CarregarHistoricoCompras(txtCodigo.Text);

            // Simula clique no localizar para recarregar o estoque na tela inicial
            txtEstoqueAtual.Text = (LerEstoqueComoInteiro(txtEstoqueAtual.Text) + qtd).ToString();
        }

        private void btnImportarXML_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("A importação de arquivo XML da Nota Fiscal Eletrônica será implementada na próxima fase.", "Módulo XML", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void btnLancarAjuste_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtCodigo.Text))
            {
                MessageBox.Show("Localize ou grave um produto primeiro para ajustar o estoque!", "Atenção", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            decimal qtd = ConverterTextoParaDecimal(txtAjusteQtd.Text);
            if (qtd <= 0)
            {
                MessageBox.Show("A quantidade deve ser maior que zero.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string tipo = cbAjusteTipo.Text;
            string motivo = txtAjusteMotivo.Text;

            BancoDeDados.RegistrarAjusteEstoque(txtCodigo.Text, tipo, qtd, motivo);
            MessageBox.Show("Ajuste de estoque concluído com sucesso!", "Balanço", MessageBoxButton.OK, MessageBoxImage.Information);

            txtAjusteQtd.Clear();
            txtAjusteMotivo.Clear();

            CarregarHistoricoEstoque(txtCodigo.Text);
        }

        // ==========================================================
        // LÓGICA DA ABA 5 (GRADE E VARIAÇÃO)
        // ==========================================================
        private void btnAdicionarVariacao_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(cbVariacaoAtributo.Text) || string.IsNullOrWhiteSpace(txtVariacaoValor.Text))
            {
                MessageBox.Show("Preencha o Atributo e o Valor para adicionar uma variação à grade.", "Atenção", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            decimal estq = string.IsNullOrWhiteSpace(txtVariacaoEstoque.Text) ? 0 : ConverterTextoParaDecimal(txtVariacaoEstoque.Text);

            ListaVariacoes.Add(new ProdutoVariacaoModel
            {
                CodigoProduto = txtCodigo.Text,
                Atributo = cbVariacaoAtributo.Text,
                Valor = txtVariacaoValor.Text,
                CodigoBarras = txtVariacaoCodigo.Text,
                Estoque = estq
            });

            cbVariacaoAtributo.Text = "";
            txtVariacaoValor.Clear();
            txtVariacaoCodigo.Clear();
            txtVariacaoEstoque.Text = "0";
        }

        // ==========================================================
        // LIMPEZA
        // ==========================================================
        private void LimparCampos()
        {
            txtCodigo.Clear(); txtDescricao.Clear(); cbUnidade.Text = "UN";
            txtPrecoCompra.Text = "0,00"; txtMargem.Text = "100,00"; txtPrecoVenda.Text = "0,00"; txtPrecoPrazo.Text = "0,00";
            txtEstoqueMinimo.Text = "0"; txtEstoqueAtual.Text = "0"; txtObservacoes.Clear();

            txtNcm.Clear();
            txtCfop.Text = "5102";
            cbCsosn.Text = "102 - Tributada pelo Simples (Sem ST)";

            caminhoFotoAtual = "";
            if (imgProduto != null) imgProduto.Source = null;
            if (gridHistoricoVendas != null) gridHistoricoVendas.ItemsSource = null;
            if (gridHistoricoCompras != null) gridHistoricoCompras.ItemsSource = null;
            if (gridHistoricoEstoque != null) gridHistoricoEstoque.ItemsSource = null;

            if (ListaVariacoes != null) ListaVariacoes.Clear();

            if (txtTotalValorVenda != null) txtTotalValorVenda.Text = "R$ 0,00";
            if (txtTotalQtdVenda != null) txtTotalQtdVenda.Text = "0,00";

            txtCodigo.Focus();
        }
    }
}