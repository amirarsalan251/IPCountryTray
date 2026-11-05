# IPCountryTray

**Language / زبان:** [English](#english) | [فارسی](#persian)

<a id="english"></a>

## English

IPCountryTray is a small Windows utility that displays your public IP address and approximate geographic location from the notification area. It can check both your direct internet connection and a separately configured proxy.

### Features

- Compact, dark popup with **DIRECT IP** and **PROXY IP** tabs.
- Public IP, country, city, region, and internet service provider (when available).
- Country code shown in the notification-area icon.
- Automatic checks every **5 minutes**, plus a manual refresh button.
- Separate proxy settings window (server, port, optional username and password).
- Automatic fallback to another lookup service when one is unavailable.
- Runs in the notification area; closing the popup does not exit the app.

### Requirements

- Windows 10 or Windows 11
- .NET Framework 4.8
- Internet access to IP lookup services

### Getting started

1. Extract the release archive and run `IPCountryTray.exe`. No installer is required.
2. Open the popup by clicking the icon in the Windows notification area.
3. Select **DIRECT IP** to view the public IP of your direct connection.
4. Select **PROXY IP**, then click the **gear icon** to enter your proxy server and port. Add credentials only if required, enable the proxy, and choose **Save & Test**.
5. Click the refresh icon to check both connections immediately. Otherwise, the checks run every five minutes.

Use the tray icon's right-click menu for refresh, copying the selected IP, proxy settings, and **Exit**.

**Proxy support:** HTTP and HTTPS proxy endpoints are supported. SOCKS4 and SOCKS5 are not supported in v1.0. The proxy tab shows the **public exit IP** seen by the lookup service; this may differ from the proxy server address. The direct tab bypasses the proxy configured in this application.

### Build from source

Run the following command in PowerShell on Windows:

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

This builds `Program.cs` into `IPCountryTray.exe` using the Windows .NET Framework C# compiler. Alternatively, open `IPCountryTray.csproj` in Visual Studio with the **.NET Framework 4.8 targeting pack** installed.

### Privacy and limitations

- Proxy settings are stored locally at `%LOCALAPPDATA%\IPCountryTray\proxy.dat`.
- The proxy **password** is encrypted using Windows DPAPI for the current Windows account. Other saved proxy fields are not encrypted.
- Public IP lookups use HTTPS requests to `ipwho.is`, `ipapi.co`, and `api.ipify.org`. The provider handling a request can see the public IP used for that connection.
- Locations are **IP-based estimates**, not GPS coordinates; city, region, and provider data may be inaccurate.
- This tool checks connectivity and public IP information. It does **not** configure a system-wide proxy or VPN.

### Version and repository

**v1.0.0** is the initial compact two-tab release. For GitHub, commit the source code, project file, build script, `.gitignore`, and this README. Publish the executable archive as a GitHub Release asset rather than committing binaries to source control.

---

<a id="persian"></a>

<h2 dir="rtl">فارسی</h2>

<div dir="rtl">

<p>برنامه <strong>IPCountryTray</strong> یک ابزار کوچک برای ویندوز است که IP عمومی اینترنت و موقعیت مکانی تقریبی آن را کنار ساعت ویندوز نمایش می‌دهد. برنامه می‌تواند اتصال مستقیم اینترنت و یک پروکسی جداگانه را مستقل از یکدیگر بررسی کند.</p>

<h3>قابلیت‌ها</h3>

<ul>
<li>پنجره کوچک با ظاهر تیره و دو تب <strong>DIRECT IP</strong> و <strong>PROXY IP</strong></li>
<li>نمایش IP عمومی، کشور، شهر، منطقه و شرکت ارائه‌دهنده اینترنت (در صورت وجود اطلاعات)</li>
<li>نمایش کد کشور در آیکن کنار ساعت ویندوز</li>
<li>بررسی خودکار هر <strong>۵ دقیقه</strong> و امکان به‌روزرسانی دستی</li>
<li>پنجره جداگانه برای تنظیم آدرس و پورت پروکسی و نام کاربری و رمز عبور اختیاری</li>
<li>استفاده از سرویس جایگزین در صورت پاسخ‌ندادن سرویس اصلی شناسایی IP</li>
<li>ادامه فعالیت کنار ساعت، حتی پس از بستن پنجره کوچک برنامه</li>
</ul>

<h3>پیش‌نیازها</h3>

<ul>
<li>ویندوز ۱۰ یا ۱۱</li>
<li>‎.NET Framework 4.8‎</li>
<li>دسترسی اینترنت برای ارتباط با سرویس‌های شناسایی IP</li>
</ul>

<h3>روش اجرا و استفاده</h3>

<ol>
<li>فایل ZIP نسخه انتشار را استخراج و <code>IPCountryTray.exe</code> را اجرا کنید؛ نیازی به نصب برنامه نیست.</li>
<li>روی آیکن برنامه کنار ساعت ویندوز کلیک کنید تا پنجره کوچک نمایش داده شود.</li>
<li>در تب <strong>DIRECT IP</strong>، IP عمومی اتصال مستقیم را مشاهده کنید.</li>
<li>برای بررسی پروکسی وارد تب <strong>PROXY IP</strong> شوید و با دکمه <strong>چرخ‌دنده</strong>، آدرس سرور، پورت و در صورت نیاز اطلاعات ورود را وارد کنید. سپس پروکسی را فعال و گزینه <strong>Save &amp; Test</strong> را انتخاب کنید.</li>
<li>با دکمه به‌روزرسانی، هر دو اتصال بلافاصله بررسی می‌شوند؛ در غیر این صورت بررسی‌ها هر پنج دقیقه انجام می‌شوند.</li>
</ol>

<p>با کلیک راست روی آیکن کنار ساعت، می‌توانید اطلاعات را به‌روزرسانی کنید، IP تب انتخاب‌شده را کپی کنید، تنظیمات پروکسی را باز کنید یا با گزینه <strong>Exit</strong> از برنامه خارج شوید.</p>

<p><strong>پشتیبانی پروکسی:</strong> در نسخه ۱٫۰، پروکسی‌های HTTP و HTTPS پشتیبانی می‌شوند؛ SOCKS4 و SOCKS5 پشتیبانی نمی‌شوند. تب پروکسی <strong>IP عمومی خروجی</strong> را نمایش می‌دهد که ممکن است با IP سرور پروکسی تفاوت داشته باشد. تب اتصال مستقیم از پروکسی تعریف‌شده داخل همین برنامه استفاده نمی‌کند.</p>

<h3>ساخت برنامه از سورس</h3>

<p>دستور زیر را در PowerShell ویندوز اجرا کنید:</p>

</div>

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

<div dir="rtl">

<p>این دستور، فایل <code>Program.cs</code> را با کامپایلر ‎C#‎ دات‌نت فریم‌ورک ویندوز به <code>IPCountryTray.exe</code> تبدیل می‌کند. همچنین می‌توانید فایل <code>IPCountryTray.csproj</code> را در Visual Studio باز کنید؛ برای این روش، بسته هدف‌گذاری <strong>.NET Framework 4.8</strong> لازم است.</p>

<h3>حریم خصوصی و محدودیت‌ها</h3>

<ul>
<li>تنظیمات پروکسی به‌صورت محلی در مسیر <code>%LOCALAPPDATA%\IPCountryTray\proxy.dat</code> ذخیره می‌شوند.</li>
<li><strong>رمز عبور</strong> پروکسی با فناوری DPAPI ویندوز برای حساب کاربری فعلی رمزگذاری می‌شود؛ سایر فیلدهای ذخیره‌شده رمزگذاری نشده‌اند.</li>
<li>برنامه از طریق HTTPS به سرویس‌های <code>ipwho.is</code>، <code>ipapi.co</code> و <code>api.ipify.org</code> درخواست می‌فرستد. هر سرویس می‌تواند IP عمومی همان اتصال را مشاهده کند.</li>
<li>موقعیت مکانی بر اساس پایگاه‌های داده IP برآورد می‌شود و <strong>موقعیت GPS نیست</strong>؛ کشور، شهر، منطقه یا ارائه‌دهنده اینترنت ممکن است دقیق نباشد.</li>
<li>برنامه فقط اطلاعات IP عمومی را بررسی می‌کند و تنظیمات VPN یا پروکسی کل سیستم را تغییر نمی‌دهد.</li>
</ul>

<h3>نسخه و انتشار</h3>

<p><strong>v1.0.0</strong> اولین نسخه رسمی با پنجره کوچک و دو تب است. برای انتشار در GitHub، سورس، فایل پروژه، اسکریپت Build، فایل <code>.gitignore</code> و همین README را در مخزن قرار دهید. بهتر است فایل اجرایی یا ZIP را به‌عنوان <strong>GitHub Release</strong> منتشر کنید، نه در تاریخچه سورس.</p>

</div>
