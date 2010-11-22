using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LoggingUtility
{
    internal enum TipoEvento
    {
        Excepcao,
        Mensagem
    }

    public class Evento
    {
        internal DateTime Momento;
        internal TipoEvento Tipo;
        internal string Mensagem;
        internal string Outros;

        internal Evento(string mensagem)
        {
            SetEvento(mensagem);
        }

        internal Evento(Exception excepcao)
        {
            SetEvento(excepcao);
        }

        internal void SetEvento(string mensagem)
        {
            Momento = DateTime.Now;
            Tipo = TipoEvento.Mensagem;
            Mensagem = mensagem;
            Outros = "";
        }

        internal void SetEvento(Exception excepcao)
        {
            Momento = DateTime.Now;
            Tipo = TipoEvento.Excepcao;
            Mensagem = excepcao.Message ?? "";
            Outros = excepcao.StackTrace ?? "";
        }

        internal string ToString(TipoLog tipo)
        {
            try
            {
                switch(tipo)
                {
                    case TipoLog.json:
                        {
                            return String.Format("{{'evento':'{0}','data':'{1}','mensagem':'{2}','outros':'{3}'}}", Tipo, Momento, Mensagem, Outros);
                        }
                    case TipoLog.texto:
                        {
                            return String.Format(@"
--> tipo: {0} ({1})
mensagem: 
{2}
outros: {3}
------------------",
                                Tipo,
                                Momento,
                                Mensagem,
                                Outros);
                        }
                    case TipoLog.textoMinimo:
                        {
                            return String.Format(@"({1})
{2}
{3}
",
Tipo,
Momento,
Mensagem,
Outros);
                        }
                    case TipoLog.xmlfragment:
                        {
                            return String.Format(@"
<evento tipo=""{0}"" data=""{1}"">
    <mensagem>{2}</mensagem>
    <outros>{3}</outros>
</evento>",
TrataStringParaXml(Tipo.ToString()),
TrataStringParaXml(Momento.ToString()),
TrataStringParaXml(Mensagem.ToString()),
TrataStringParaXml(Outros.ToString()));
                        }
                }
            }
            catch
            {
                Console.WriteLine("Poop");
            }
            return "";
        }

        private static string TrataStringParaXml(string texto)
        {
            return texto.Replace("\"", "\\\"");
        }
    }
}
