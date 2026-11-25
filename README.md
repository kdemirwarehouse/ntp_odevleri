# YMH219 — Nesne Tabanlı Programlama Ödevleri

Nesne Tabanlı Programlama dersindeki ödevlerimin hepsini bir arada tutan depo.

| | |
|---|---|
| **Öğrenci** | Abdulkadir Demir |
| **Öğrenci numarası** | 240541007 |
| **Dil / Ortam** | C# · .NET 10 (LTS) · Visual Studio 2026 |

## Haftalık laboratuvar ödevleri

**[Hafta 5 — Evim](Hafta5_Evim/README.md)**: sınıflar, doğrulamalı property, yapıcı metotlar, statik sayaç ve `static readonly` adres. Konsol uygulaması, 22 davranış testi, föy ve teslim raporları bu klasörde.

- [Word raporu](Hafta5_Evim/teslim/Hafta5_NTP_Odev.docx)
- [PDF raporu](Hafta5_Evim/teslim/Hafta5_NTP_Odev.pdf)
- [Teslim ZIP'i](Hafta5_Evim/teslim/240541007_NTP_Hafta5.zip)

## Uygulama ödevleri (Windows Forms)

Bu altı proje ayrıca kendi bağımsız depolarında da duruyor. Buradaki klasörler o depoların kaynak kodunun kopyası. Her proje kendi çözüm dosyası ve README'siyle bağımsız derlenebilir.

| Klasör | Uygulama | Bağımsız depo |
|---|---|---|
| [YMH219-FlappyBird](YMH219-FlappyBird/README.md) | Timer, fizik, çarpışma, skor | [kdemirwarehouse/YMH219-FlappyBird](https://github.com/kdemirwarehouse/YMH219-FlappyBird) |
| [YMH219-NotepadApp](YMH219-NotepadApp/README.md) | Not defteri, File I/O, JSON serileştirme | [kdemirwarehouse/YMH219-NotepadApp](https://github.com/kdemirwarehouse/YMH219-NotepadApp) |
| [YMH219-KeyEventLogger](YMH219-KeyEventLogger/README.md) | Yalnızca kendi penceresindeki klavye olayları | [kdemirwarehouse/YMH219-KeyEventLogger](https://github.com/kdemirwarehouse/YMH219-KeyEventLogger) |
| [YMH219-ChatApp](YMH219-ChatApp/README.md) | async/await ile TCP istemci-sunucu sohbet | [kdemirwarehouse/YMH219-ChatApp](https://github.com/kdemirwarehouse/YMH219-ChatApp) |
| [YMH219-SpeechApp](YMH219-SpeechApp/README.md) | System.Speech ile TTS ve STT | [kdemirwarehouse/YMH219-SpeechApp](https://github.com/kdemirwarehouse/YMH219-SpeechApp) |
| [YMH219-FaceRecognition](YMH219-FaceRecognition/README.md) | OpenCvSharp ile Haar algılama ve LBPH tanıma | [kdemirwarehouse/YMH219-FaceRecognition](https://github.com/kdemirwarehouse/YMH219-FaceRecognition) |

## Hızlı başlangıç

```powershell
git clone https://github.com/kdemirwarehouse/ntp_odevleri.git
cd ntp_odevleri

# Hafta 5
dotnet run --project Hafta5_Evim/Hafta5_Evim.csproj

# Herhangi bir Windows Forms uygulaması, örnek:
dotnet build YMH219-ChatApp/ChatApp.sln
```

Gereksinim: [.NET 10 SDK](https://dotnet.microsoft.com/download). Windows Forms projeleri yalnızca Windows'ta çalışır.
