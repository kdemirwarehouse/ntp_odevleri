using System;

namespace Hafta5_Evim
{
    public class Kisi
    {
        // Statik sayaç: nesneye değil sınıfa aittir, tüm Kisi nesneleri paylaşır.
        public static int KisiSayisi = 0;

        // Görev 3: tüm kişiler aynı evde oturduğu için adres sınıfa ait sabit bir değerdir.
        public static readonly string EvAdresi = "Atatürk Mah. Cumhuriyet Cad. No:23 Daire:5, Merkez / Muş";

        private int dogumYili;
        private string meslek = "Belirtilmedi";

        public string Ad { get; set; }

        // Görev 1: Meslek artık doğrulamalı property. Boş girilirse "Belirtilmedi" atanır.
        public string Meslek
        {
            get { return meslek; }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    meslek = "Belirtilmedi";
                else
                    meslek = value.Trim();
            }
        }

        public int DogumYili
        {
            get { return dogumYili; }
            set
            {
                if (value > 1900 && value <= DateTime.Now.Year)
                    dogumYili = value;
                else
                    Console.WriteLine("Geçersiz doğum yılı!");
            }
        }

        // Ödev (gelecek hafta hazırlığı): yapıcı metot aşırı yüklemesi.
        public Kisi(string ad) : this(ad, DateTime.Now.Year, "")
        {
        }

        public Kisi(string ad, int dogumYili, string meslek)
        {
            Ad = ad;
            DogumYili = dogumYili;   // property set bloğundan geçer (doğrulama)
            Meslek = meslek;         // property set bloğundan geçer (boş kontrolü)
            KisiSayisi++;            // her yeni kişi oluştukça artar
        }

        public int YasHesapla()
        {
            return DateTime.Now.Year - dogumYili;
        }

        public void BilgiYaz()
        {
            Console.WriteLine($"{Ad} - {Meslek} - {YasHesapla()} yaşında");
        }
    }
}
