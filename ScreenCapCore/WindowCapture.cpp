
/////////////////////////////////////////////////////////////

#pragma unmanaged

#include <stdio.h>
#include <Windows.h>

#pragma comment (lib, "user32.lib")
#pragma comment (lib, "gdi32.lib")

#pragma managed

/////////////////////////////////////////////////////////////

const int BmpHeaderByteCount = 54;

namespace ReCaD
{
	public ref class WindowCapture
	{
	private:
		::HWND         m_wnd;
		::HDC          m_wndDc;
		::HBITMAP      m_bmp;
		::HDC          m_bmpDc;
		System::IntPtr m_bmpIntPtr;
		bool           m_initialized;
		int            m_width;
		int            m_height;
		int            m_bmpDataByteCount;
		int            m_bmpFullDataByteCount;
		unsigned char* m_header;

		void SetSize(int w, int h)
		{
			m_width = w;
			m_height = h;

			m_bmpDataByteCount = w * h * 4;
			m_bmpFullDataByteCount = m_bmpDataByteCount + BmpHeaderByteCount;

			// Setup capture bitmap
			m_bmp = ::CreateCompatibleBitmap(m_wndDc, w, h);
			m_bmpIntPtr = System::IntPtr(m_bmp);

			// Setup bitmap dc
			m_bmpDc = ::CreateCompatibleDC(nullptr);
			::SelectObject(m_bmpDc, m_bmp);

			// Store bitmap header
			auto temp_bmp = System::Drawing::Bitmap::FromHbitmap(m_bmpIntPtr);
			auto ms = gcnew System::IO::MemoryStream(m_bmpFullDataByteCount);
			temp_bmp->Save(ms, System::Drawing::Imaging::ImageFormat::Bmp);
			ms->Seek(0, System::IO::SeekOrigin::Begin);
			m_header = new unsigned char[BmpHeaderByteCount];
			for (int i = 0; i < BmpHeaderByteCount; ++i)
			{
				m_header[i] = static_cast<unsigned char>(ms->ReadByte());
			}
			auto bfh = reinterpret_cast<::BITMAPFILEHEADER*>(m_header);
			auto bih = reinterpret_cast<::BITMAPINFOHEADER*>(m_header + BmpHeaderByteCount - sizeof (::BITMAPINFOHEADER));
			bih->biHeight = -bih->biHeight;
			//ms->Dispose();
			//temp_bmp->Dispose();
		}

	internal:
		WindowCapture(::HWND wnd)
			: m_wnd(wnd)
		{
			m_initialized = false;

			if (!wnd)
			{
				throw gcnew System::ArgumentException("wnd");
			}

			::WINDOWPLACEMENT wp;
			::GetWindowPlacement(wnd, &wp);
			if (SW_SHOWMINIMIZED == wp.showCmd)
			{
				wp.showCmd = SW_RESTORE;
				::SetWindowPlacement(wnd, &wp);
			}

			m_wndDc = ::GetDC(wnd);

			::RECT wnd_rect;
			::GetClientRect(wnd, &wnd_rect);

			SetSize(
				wnd_rect.right - wnd_rect.left,
				wnd_rect.bottom - wnd_rect.top);

			m_initialized = true;
		}

		void ChangeSize(int w, int h)
		{
			::DeleteDC(m_bmpDc);
			::DeleteObject(m_bmp);
			SetSize(w, h);
			SizeChanged(this, System::EventArgs::Empty);
		}

	public:
		static WindowCapture^ FromPoint(System::Drawing::Point screenPosition)
		{
			::POINT screen_point;
			screen_point.x = screenPosition.X;
			screen_point.y = screenPosition.Y;
			::HWND wnd = GetWindowAt(screen_point);
			return gcnew WindowCapture(wnd);
		}

		static WindowCapture^ FromHandle(System::IntPtr ptr)
		{
			return gcnew WindowCapture((::HWND)ptr.ToPointer());
		}

		~WindowCapture()
		{
			this->!WindowCapture();
		}

		!WindowCapture()
		{
			if (m_initialized)
			{
				m_initialized = false;
				delete[] m_header;
				::DeleteDC(m_bmpDc);
				::DeleteObject(m_bmp);
				::ReleaseDC(m_wnd, m_wndDc);
			}
		}

		event System::EventHandler^ SizeChanged;

		property System::Int32 Width
		{
			System::Int32 get()
			{
				return m_width;
			}
		}

		property System::Int32 Height
		{
			System::Int32 get()
			{
				return m_height;
			}
		}

		System::IntPtr CaptureBitmapHandle()
		{
			Capture();
			return m_bmpIntPtr;
		}

		System::Drawing::Bitmap^ CaptureBitmap()
		{
			Capture();
			return System::Drawing::Bitmap::FromHbitmap(m_bmpIntPtr);
		}

		System::Void CaptureIntoBuffer(cli::array<System::Byte>^ buffer)
		{
			Capture();
			cli::pin_ptr<unsigned char> dest = &buffer[0];
			::memcpy(dest, m_header, BmpHeaderByteCount);
			::GetBitmapBits(m_bmp, m_bmpDataByteCount, dest + BmpHeaderByteCount);
		}

		System::Void CaptureIntoBufferNoHeader(cli::array<System::Byte>^ buffer)
		{
			Capture();
			cli::pin_ptr<unsigned char> dest = &buffer[0];
			::GetBitmapBits(m_bmp, m_bmpDataByteCount, dest);
		}

		System::Int32 GetRequiredByteCount()
		{
			return m_bmpFullDataByteCount;
		}

		System::Int32 GetRequiredByteCountNoHeader()
		{
			return m_bmpDataByteCount;
		}

	private:
		void Capture()
		{
			::RECT client_rect;
			::GetClientRect(m_wnd, &client_rect);
			int w = client_rect.right - client_rect.left;
			int h = client_rect.bottom - client_rect.top;
			if ((w != m_width) || (h != m_height))
			{
				ChangeSize(w, h);
			}

			::BitBlt(m_bmpDc, 0, 0, m_width, m_height, m_wndDc, 0, 0, SRCCOPY);
		}

		static ::HWND GetWindowAt(::HWND parent, ::POINT screenPt)
		{
			::RECT screen_r;
			::GetWindowRect(parent, &screen_r);
			::POINT local_pt;
			local_pt.x = screenPt.x - screen_r.left;
			local_pt.y = screenPt.y - screen_r.top;

			::HWND wnd = ::ChildWindowFromPoint(parent, local_pt);
			if (wnd && (wnd != parent))
			{
				return GetWindowAt(wnd, screenPt);
			}
			else
			{
				return parent;
			}
		}

		static ::HWND GetWindowAt(::POINT screenPt)
		{
			auto wnd = ::WindowFromPoint(screenPt);

#if 1
			auto x = wnd;
			while (x)
			{
				wnd = x;
				x = ::GetParent(x);
			}
#else
			if (wnd)
			{
				wnd = GetWindowAt(wnd, screenPt);
			}
#endif

			return wnd;
		}
	};
}
