<%@ Page Language="C#" MasterPageFile="~/Master.master" AutoEventWireup="true" CodeFile="index2.aspx.cs" Inherits="index2" %>

<asp:Content ID="Content2" ContentPlaceHolderID="content" Runat="Server">
		<h3>Build matrix</h3>

		<div id="buildtable" runat="server">
			
		</div>
				
		<h3>Legend</h3>
		<div>
		<table class='buildstatus'><tr><td class='enabled'>Enabled lane/host</td><td class='disabled'>Disabled lane/host</td></tr></table>
		</div>
</asp:Content>

