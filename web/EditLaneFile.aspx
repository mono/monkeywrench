<%@ Page MasterPageFile="~/Master.master" Language="C#" AutoEventWireup="true" CodeFile="EditLaneFile.aspx.cs" Inherits="EditLaneFile" %>

<asp:Content ID="Content2" ContentPlaceHolderID="content" runat="Server">
    <div>
        <asp:TextBox ID="txtEditor" runat="server" Height="600px" TextMode="MultiLine" Width="800px"></asp:TextBox>
        <br />
        <asp:Button ID="cmdSave" runat="server" OnClick="cmdSave_Click" Text="Save" />
        <asp:Button ID="cmdCancel" runat="server" OnClick="cmdCancel_Click" Text="Cancel" />
        <h5>
            Environment variables set by the builder:</h5>
            <pre>
BUILD_REPOSITORY:   The svn repository of this lane. (Exactly as configured for the lane, if the lane has multiple svn repositories separated by commas, this variable will have it too).
BUILD_DATA_LANE:    The directory where this lane puts its data.
BUILD_INSTALL:      The install directory (typically BUILDER_DATA_LANE/BUILD_REVISION/install)
BUILD_REVISION:     The revision currently being built.
BUILD_COMMAND:      The command currently being executed.
BUILD_LANE:         The lane currently being built.
BUILD_SEQUENCE:     For parallelizable commands, this is the sequence of the command. For instance:
                    You have 3 commands which can run in parallel. The first one gets BUILD_SEQUENCE=0,
                    the second BUILD_SEQUENCE=1, etc.

PATH:               BUILD_INSTALL/bin:PATH
LD_LIBRARY_PATH     BUILD_INSTALL/lib:LD_LIBRARY_PATH
PKG_CONFIG_PATH     BUILD_INSTALL/lib/pkgconfig:PKG_CONFIG_PATH

SVN_REPOSITORY:     (Obsolete) The svn repository of this lane. (Exactly as configured for the lane, if the lane has multiple svn repositories separated by commas, this variable will have it too).
BUILDER_DATA_LANE:  (Obsolete) The directory where this lane puts its data.
BUILD_STEP:         (Obsolete) BUILD_COMMAND.Replace (".sh", "")
        </pre>
    </div>
</asp:Content>
