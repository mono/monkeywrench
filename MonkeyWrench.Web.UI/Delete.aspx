<%@ Page  MasterPageFile="~/Master.master" Language="C#" AutoEventWireup="true" CodeBehind="Delete.aspx.cs" Inherits="Delete" %>

<asp:Content ID="Content2" ContentPlaceHolderID="content" runat="Server">
<center>
    <h2 style="color: Red">Confirm deletion</h2>
    <div>
        <asp:Label ID="lblMessage" runat="server" Font-Bold="True" Font-Size="X-Large" ForeColor="Red" ></asp:Label>
        <br />
        <br />
        <asp:Button ID="cmdConfirm" runat="server" Text="Yes" Enabled="false" onclick="cmdConfirm_Click" Width="200px" />
        <asp:Label ID="lblDummy" runat="server" Width="100px" />
        <asp:Button ID="cmdCancel" runat="server" Text="No / Cancel" onclick="cmdCancel_Click" Width="200px"/>
        <asp:HiddenField ID="txtReturnTo" runat="server" />
    </div>
    </center>
</asp:Content>
