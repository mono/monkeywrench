<%@ Page MasterPageFile="~/Master.master" Language="C#" AutoEventWireup="true" Inherits="Notifications"
	CodeBehind="Notifications.aspx.cs" EnableViewState="false" %>

<asp:Content ID="Content2" ContentPlaceHolderID="content" runat="Server" EnableViewState="false">
	<asp:Table runat="server" CssClass="center">
		<asp:TableRow>
			<asp:TableCell HorizontalAlign="Center">
				<table class="buildstatus notifications">
					<thead>
						<tr>
							<th colspan="6">Notifications</th>
						</tr>
						<tr>
							<th>Name</th>
							<th>Identity</th>
							<th>Identity Type</th>
							<th>Mode</th>
							<th>Notification Type</th>
							<th>Actions</th>
						</tr>
					</thead>
					
					<tbody>
						<asp:Repeater id="notificationsRepeater" runat="server">
						<ItemTemplate>
							<tr>
								<td><%# Eval("name") %></td>
								<td><%# Eval("identName") %></td>
								<td><%# Eval("identType") %></td>
								<td><%# Eval("mode") %></td>
								<td><%# Eval("type") %></td>
								<td><asp:LinkButton ID="notificationRemove" Text="Remove" runat="server" OnCommand="notificationRemove_remove" CommandArgument='<%# Eval("id") %>' /></td>
							</tr>
						</ItemTemplate>
						</asp:Repeater>
					</tbody>
					
					<tfoot>
						<tr>
							<td>
								<asp:TextBox ID="txtName" runat="server" ToolTip="&nbsp;" />
							</td>
							<td colspan="2">
								<asp:DropDownList ID="cmbIdentity" runat="server" ToolTip="The identity to use for this notification" />
							</td>
							<td>
								<asp:DropDownList ID="cmbMode" runat="server" ToolTip="Notification mode. 'Default' will just report the failure to the committer. 
'MoonlightDrt' and 'NUnit' will try to find the corresponding xml file with test results, 
and report to test owner (if known) or committer with more information about which tests failed.
Ignored for GitHub.">
									<asp:ListItem Text="Default" Value="0" />
									<asp:ListItem Text="MoonlightDrt" Value="1" />
									<asp:ListItem Text="NUnit" Value="2" />
								</asp:DropDownList>
							</td>
							<td>
								<asp:DropDownList ID="cmbNotificationType" runat="server" ToolTip="Notification type (ignored for GitHub)">
									<asp:ListItem Text="Fatal failures only" Value="0" />
									<asp:ListItem Text="Nonfatal failures only" Value="1" />
									<asp:ListItem Text="All failures" Value="2" />
								</asp:DropDownList>
							</td>
							<td>
								<asp:LinkButton ID="lnkAdd" Text="Add" runat="server" OnClick="lnkAdd_Click" />
							</td>
						</tr>
					</tfoot>
				</table>
			</asp:TableCell>
		</asp:TableRow>
		<asp:TableRow>
			<asp:TableCell HorizontalAlign="Center">
				<div id="lblHelp" style="color: Green">&nbsp;</div>
			</asp:TableCell>
		</asp:TableRow>
		<asp:TableRow>
			<asp:TableCell HorizontalAlign="Center">
				<asp:Label ID="lblMessage" ForeColor="Red" runat="server" />
			</asp:TableCell></asp:TableRow>
	</asp:Table>
</asp:Content>
