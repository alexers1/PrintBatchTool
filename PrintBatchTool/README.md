# PrintBatchTool Version1.0

A batch file printing utility for Windows designed to simplify printing multiple documents in different formats from a single directory.

default printer:\\svzf2080\PRZF0001

## Features

- **Printer Management**: Detect and select available printers on your system
- **Batch File Scanning**: Automatically scan directories for printable documents
- **Multi-Format Support**: Support for common document formats:
  - PDF
  - Microsoft Office (DOC, DOCX, XLS, XLSX, PPT, PPTX)
  - Text files (TXT)
- **Flexible File Selection**: 
  - Auto-scan by file type using checkboxes
  - Manually add individual files to the print queue
- **Print Queue Management**: View, customize, and clear the list of files awaiting printing
- **Configurable Print Settings**: Adjust print parameters before execution

## System Requirements

- **OS**: Windows 7 or later
- **.NET Runtime**: .NET 8 (included in standalone executable)
- **Processor**: x64 architecture

## Installation

1. Download the latest executable from the releases section
2. No additional installation required — the application runs as a standalone executable

## How to Use

1. **Select a Printer**
   - The application automatically detects your default printer
   - Change the printer using the "重新指定打印机" (Reassign Printer) button if needed

2. **Choose a Directory**
   - Click "选择目录" (Select Directory) to browse for a folder containing documents
   - The application scans the folder for supported file types

3. **Select Files to Print**
   - Use the checkboxes to filter files by type
   - Manually add files using the "+ 手动添加文件" (+ Add Files Manually) button
   - Remove or clear files from the print queue as needed

4. **Configure Print Settings**
   - Set print options in the "打印配置与执行" (Print Configuration & Execution) section
   - Click the print button to start batch printing


## Technology Stack

- **.NET 8** with WPF (Windows Presentation Foundation)
- **Windows Forms** for file/directory dialogs
- **Self-contained deployment** for compatibility across Windows systems

## License

Free to use

## Support

For issues, questions, or feature requests, please open an issue on the repository.
