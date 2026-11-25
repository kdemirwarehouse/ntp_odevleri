using System;
using System.Collections.Generic;

namespace Hafta5_Evim
{
    public class Ev
    {
        private List<Kisi> uyeler = new List<Kisi>();

        public int UyeSayisi
        {
            get { return uyeler.Count; }
        }

        public void UyeEkle(Kisi k)
        {
            uyeler.Add(k);
        }

        public void TumUyeleriYaz()
        {
            foreach (Kisi k in uyeler)
                k.BilgiYaz();
        }

        public double OrtalamaYas()
        {
            if (uyeler.Count == 0) return 0;
            int toplam = 0;
            foreach (Kisi k in uyeler)
                toplam += k.YasHesapla();
            return (double)toplam / uyeler.Count;
        }

        // Görev 2: en yaşlı üyeyi bulup döndürür. Ev boşsa null döner.
        public Kisi EnYasli()
        {
            if (uyeler.Count == 0) return null;
            Kisi enYasli = uyeler[0];
            foreach (Kisi k in uyeler)
            {
                if (k.YasHesapla() > enYasli.YasHesapla())
                    enYasli = k;
            }
            return enYasli;
        }
    }
}
