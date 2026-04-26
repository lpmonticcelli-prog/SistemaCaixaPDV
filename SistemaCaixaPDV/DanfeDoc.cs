// Se o visual studio reclamar do namespace acima, troque para: using Zeus.Net.NFe.Danfe.QuestPdf;

namespace SistemaCaixaPDV
{
    internal class DanfeDoc
    {
        private string caminhoCompleto;

        public DanfeDoc(string caminhoCompleto)
        {
            this.caminhoCompleto = caminhoCompleto;
        }

        internal void ExportarPdf(string caminhoPdf)
        {
            throw new NotImplementedException();
        }

        internal object Gerar()
        {
            throw new NotImplementedException();
        }
    }
}