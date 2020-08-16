using System.Runtime.InteropServices;

namespace System.Windows.Forms
{
	public class ListViewEx : ListView
	{
		public ListViewEx()
		{
			SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
			SetStyle(ControlStyles.EnableNotifyMessage, value: true);
			SendMessage(base.Handle, 295, 65537, 0);
		}

		protected override void OnNotifyMessage(Message m)
		{
			if (m.Msg != 20)
			{
				base.OnNotifyMessage(m);
			}
		}

		protected override void OnClick(EventArgs e)
		{
			base.OnClick(e);
			SendMessage(base.Handle, 295, 65537, 0);
		}

		protected override void OnKeyUp(KeyEventArgs e)
		{
			base.OnKeyUp(e);
			SendMessage(base.Handle, 295, 65537, 0);
		}

		[DllImport("user32.dll")]
		public static extern int SendMessage(IntPtr hWnd, int wMsg, int wParam, int lParam);
	}
}
