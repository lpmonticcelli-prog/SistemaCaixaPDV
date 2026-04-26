using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;

namespace SistemaCaixaPDV // <-- Verifique o nome do projeto
{
    public static class Seguranca
    {
        // 1. Pega o número único físico da máquina (MAC Address)
        public static string ObterCodigoMaquina()
        {
            string macAddress = "";
            foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                // Pega a primeira placa de rede ativa
                if (nic.OperationalStatus == OperationalStatus.Up && nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                {
                    macAddress = nic.GetPhysicalAddress().ToString();
                    break;
                }
            }
            if (string.IsNullOrEmpty(macAddress)) macAddress = "PC-DESCONHECIDO";
            return macAddress;
        }

        // 2. A MÁGICA: Pega o código do PC + sua palavra secreta e gera a licença válida
        public static string GerarChaveMestra(string codigoMaquina)
        {
            // ATENÇÃO: Essa é a sua assinatura digital. Mude para algo seu!
            string palavraSecreta = "WESLEY_PDV_PRO_2026";

            string texto = codigoMaquina + palavraSecreta;

            // Cria um Hash (uma criptografia irreversível)
            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.ASCII.GetBytes(texto);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }

                // Formata a chave para ficar com cara de licença profissional (Ex: A1B2-C3D4-E5F6)
                string hash = sb.ToString().Substring(0, 12);
                return $"{hash.Substring(0, 4)}-{hash.Substring(4, 4)}-{hash.Substring(8, 4)}";
            }
        }

        // 3. Verifica se a chave que o cliente digitou bate com a máquina dele
        public static bool ValidarLicenca(string chaveDigitada)
        {
            if (string.IsNullOrWhiteSpace(chaveDigitada)) return false;

            string chaveEsperada = GerarChaveMestra(ObterCodigoMaquina());
            return chaveDigitada.Trim().ToUpper() == chaveEsperada.ToUpper();
        }
    }
}