<%@ Page MasterPageFile="~/Master.master" Language="C#" AutoEventWireup="true" Inherits="EditLaneFile" Codebehind="EditLaneFile.aspx.cs" %>

<asp:Content ID="Content2" ContentPlaceHolderID="content" runat="Server">
    <div>
        <asp:TextBox ID="txtEditor" runat="server" Height="600px" TextMode="MultiLine" Width="800px"></asp:TextBox>
        <br />
        <asp:Button ID="cmdSave" runat="server" OnClick="cmdSave_Click" Text="Save" />
        <asp:Button ID="cmdCancel" runat="server" OnClick="cmdCancel_Click" Text="Cancel" />
        <h5>This file is used by the following lanes:</h5>
        <asp:ListBox ID="lstLanes" runat="server"></asp:ListBox>
        <h5>
            Environment variables set by the builder:</h5>
            <pre>
BUILD_REPOSITORY:   The svn repository of this lane. (Exactly as configured for the lane, if the lane has multiple svn repositories separated by commas, this variable will have it too).
BUILD_REPOSITORY_0: Each individial repository of BUILD_REPOSITORY (named _0, _1, etc)
BUILD_REPOSITORY_SPACE: Same as BUILD_REPOSITORY, just with space instead of comma separating repositories.
BUILD_DATA_LANE:    The directory where this lane puts its data.
BUILD_INSTALL:      The install directory (typically BUILD_DATA_LANE/BUILD_REVISION/install)
BUILD_REVISION:     The revision currently being built.
BUILD_COMMAND:      The command currently being executed.
BUILD_LANE:         The lane currently being built.
BUILD_SEQUENCE:     For parallelizable commands, this is the sequence of the command. For instance:
                    You have 3 commands which can run in parallel. The first one gets BUILD_SEQUENCE=0,
                    the second BUILD_SEQUENCE=1, etc.
BUILD_SCRIPT_DIR:   The directory the script files for the lane are stored before executing them.

PATH:               BUILD_INSTALL/bin:PATH
LD_LIBRARY_PATH     BUILD_INSTALL/lib:LD_LIBRARY_PATH
PKG_CONFIG_PATH     BUILD_INSTALL/lib/pkgconfig:PKG_CONFIG_PATH
C_INCLUDE_PATH      BUILD_INSTALL/include
CPLUS_INCLUDE_PATH  BUILD_INSTALL/include
        </pre>
        <h5>Dependencies</h5>
        <pre>Dependencies are downloaded into the directory BUILD_DATA_LANE/BUILD_REVISION/dependencies/[dependent lane]/</pre>
    </div>
</asp:Content>
