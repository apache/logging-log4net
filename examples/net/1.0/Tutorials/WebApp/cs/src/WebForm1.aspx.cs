#region Copyright
// 
// This framework is based on log4j see http://jakarta.apache.org/log4j
// Copyright (C) The Apache Software Foundation. All rights reserved.
//
// This software is published under the terms of the Apache Software
// License version 1.1, a copy of which has been included with this
// distribution in the LICENSE.txt file.
// 
#endregion

using System;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Web;
using System.Web.SessionState;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.HtmlControls;

using log4net;

namespace WebApp
{
	/// <summary>
	/// Summary description for WebForm1.
	/// </summary>
	public class WebForm1 : System.Web.UI.Page
	{
		private static readonly ILog log = LogManager.GetLogger(typeof(WebForm1));

		protected System.Web.UI.WebControls.Label Label1;
		protected System.Web.UI.WebControls.Label Label2;
		protected System.Web.UI.WebControls.Label Label3;
		protected System.Web.UI.WebControls.Label Label4;

		protected System.Web.UI.WebControls.TextBox txtAdd1;
		protected System.Web.UI.WebControls.TextBox txtAdd2;
		protected System.Web.UI.WebControls.TextBox txtAdd3;
		protected System.Web.UI.WebControls.Button btnCalcAdd;

		protected System.Web.UI.WebControls.TextBox txtSub1;
		protected System.Web.UI.WebControls.TextBox txtSub2;
		protected System.Web.UI.WebControls.TextBox txtSub3;
		protected System.Web.UI.WebControls.Button btnCalcSub;

		protected SimpleModule.Math m_MathAdd = new SimpleModule.Math();
		protected SharedModule.Math m_MathSub = new SharedModule.Math();
	
		private void Page_Load(object sender, System.EventArgs e)
		{
			// Put user code to initialize the page here
		}

		#region Web Form Designer generated code
		override protected void OnInit(EventArgs e)
		{
			AppDomain appDom = AppDomain.CurrentDomain;
			HttpContext context = HttpContext.Current;
			//
			// CODEGEN: This call is required by the ASP.NET Web Form Designer.
			//
			InitializeComponent();
			base.OnInit(e);

			txtAdd1.Text = "0";
			txtAdd2.Text = "0";
			txtAdd3.Text = "0";

			txtSub1.Text = "0";
			txtSub2.Text = "0";
			txtSub3.Text = "0";
		}
		
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{    
			this.btnCalcAdd.Click += new System.EventHandler(this.btnCalcAdd_Click);
			this.btnCalcSub.Click += new System.EventHandler(this.btnCalcSub_Click);
			this.Load += new System.EventHandler(this.Page_Load);
		}
		#endregion

		private void btnCalcAdd_Click(object sender, System.EventArgs e)
		{
			if (log.IsDebugEnabled) log.Debug("txtAdd1=[" + txtAdd1.Text + "] txtAdd2=[" + txtAdd2.Text + "]");

			int result = m_MathAdd.Add(int.Parse(txtAdd1.Text), int.Parse(txtAdd2.Text));

			if (log.IsInfoEnabled) log.Info("result=[" + result + "]");
			
			txtAdd3.Text = result.ToString();
		}

		private void btnCalcSub_Click(object sender, System.EventArgs e)
		{
			if (log.IsDebugEnabled) log.Debug("txtSub1=[" + txtSub1.Text + "] txtSub2=[" + txtSub2.Text + "]");

			int result = m_MathSub.Subtract(int.Parse(txtSub1.Text), int.Parse(txtSub2.Text));

			if (log.IsInfoEnabled) log.Info("result=[" + result + "]");
			
			txtSub3.Text = result.ToString();
		}
	}
}
