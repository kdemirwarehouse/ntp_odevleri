using System;

namespace Hafta5_Evim
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            Ev evim = new Ev();

            // Görev 4: en az 4 kişi
            evim.UyeEkle(new Kisi("Anne", 1970, "Öğretmen"));
            evim.UyeEkle(new Kisi("Baba", 1968, "Mühendis"));
            evim.UyeEkle(new Kisi("Ben", 2004, "Öğrenci"));
            evim.UyeEkle(new Kisi("Dede", 1945, "Emekli"));
            evim.UyeEkle(new Kisi("Kardeş", 2012, ""));   // boş meslek -> "Belirtilmedi"

            Console.WriteLine($"Ev adresi    : {Kisi.EvAdresi}");
            Console.WriteLine();
            Console.WriteLine("--- EV ÜYELERİ ---");
            evim.TumUyeleriYaz();
            Console.WriteLine();

            Kisi enYasli = evim.EnYasli();
            Console.WriteLine($"Ortalama yaş : {evim.OrtalamaYas():F1}");
            if (enYasli != null)
                Console.WriteLine($"En yaşlı     : {enYasli.Ad} ({enYasli.YasHesapla()} yaşında)");
            Console.WriteLine($"Toplam kişi  : {Kisi.KisiSayisi}");
        }
    }
}
