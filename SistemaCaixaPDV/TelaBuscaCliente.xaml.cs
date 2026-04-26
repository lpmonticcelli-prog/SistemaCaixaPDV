using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using Microsoft.Data.Sqlite;

namespace SistemaCaixaPDV
{
    public partial class TelaBuscaCliente : Window
    {
        private string connectionString = BancoDeDados.ConnectionString;
        public int IdSelecionado { get; set; } = 0;
        public ClienteBusca ClienteSelecionado { get; set; } = null;

        public TelaBuscaCliente()
        {
            InitializeComponent();
            CarregarClientes("");
            txtBusca.Focus();
        }

        private void CarregarClientes(string termoBusca)
        {
            var lista = new List<ClienteBusca>();
            try
            {
                using (var cx = new SqliteConnection(connectionString))
                {
                    cx.Open();
                    // BUSCANDO DA TABELA NOVA!
                    string sql = "SELECT Id, Nome, CpfCnpj, Celular FROM Clientes WHERE Nome LIKE @busca OR CpfCnpj LIKE @busca ORDER BY Nome LIMIT 50";
                    using (var cmd = new SqliteCommand(sql, cx))
                    {
                        cmd.Parameters.AddWithValue("@busca", "%" + termoBusca + "%");
                        using (var r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                lista.Add(new ClienteBusca
                                {
                                    Id = Convert.ToInt32(r["Id"]),
                                    Nome = r["Nome"].ToString(),
                                    Cpf = r["CpfCnpj"].ToString(),
                                    Telefone = r["Celular"].ToString()
                                });
                            }
                        }
                    }
                }
                gridClientes.ItemsSource = lista;
            }
            catch { }
        }

        private void btnBuscar_Click(object sender, RoutedEventArgs e) { CarregarClientes(txtBusca.Text.Trim()); }
        private void txtBusca_KeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Enter) CarregarClientes(txtBusca.Text.Trim()); }

        private void gridClientes_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (gridClientes.SelectedItem is ClienteBusca clienteClick)
            {
                this.IdSelecionado = clienteClick.Id;
                this.ClienteSelecionado = clienteClick;
                this.Close();
            }
        }
    }
}

