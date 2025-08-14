using ClosedXML.Excel;
using NphiesBridge.Shared.DTOs;
using System.Data;
using System.Drawing;

namespace NphiesBridge.Web.Services
{
    public class ExcelTemplateService
    {
        public byte[] GenerateIcdMappingTemplate()
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("ICD Code Template");

            // Define column headers
            var headers = new[]
            {
                "ICD-10-AM Code",
                "Hospital ICD Code",
                "Diagnosis Name",
                "Diagnosis Description"
            };

            // Add headers
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = worksheet.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.LightBlue;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            // Add sample data
            var sampleData = new[]
            {
                new { IcdAm = "E11.9", HospitalCode = "DM2", DiagnosisName = "Type 2 Diabetes Mellitus", Description = "Type 2 diabetes mellitus without complications" },
                new { IcdAm = "I10", HospitalCode = "HTN", DiagnosisName = "Essential Hypertension", Description = "Primary hypertension without complications" },
                new { IcdAm = "", HospitalCode = "COPD01", DiagnosisName = "Chronic Obstructive Pulmonary Disease", Description = "COPD with acute exacerbation" },
                new { IcdAm = "", HospitalCode = "MI001", DiagnosisName = "Myocardial Infarction", Description = "Acute ST-elevation myocardial infarction" }
            };

            for (int i = 0; i < sampleData.Length; i++)
            {
                var row = i + 2;
                worksheet.Cell(row, 1).Value = sampleData[i].IcdAm;
                worksheet.Cell(row, 2).Value = sampleData[i].HospitalCode;
                worksheet.Cell(row, 3).Value = sampleData[i].DiagnosisName;
                worksheet.Cell(row, 4).Value = sampleData[i].Description;

                // Style sample data
                for (int col = 1; col <= 4; col++)
                {
                    worksheet.Cell(row, col).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    if (col == 2 || col == 3) // Required columns
                    {
                        worksheet.Cell(row, col).Style.Fill.BackgroundColor = XLColor.LightYellow;
                    }
                }
            }

            // Add instructions sheet
            var instructionsSheet = workbook.Worksheets.Add("Instructions");
            instructionsSheet.Cell(1, 1).Value = "ICD-10 Code Mapping Template Instructions";
            instructionsSheet.Cell(1, 1).Style.Font.Bold = true;
            instructionsSheet.Cell(1, 1).Style.Font.FontSize = 16;

            var instructions = new[]
            {
                "",
                "Column Descriptions:",
                "",
                "1. ICD-10-AM Code (Optional):",
                "   - If you already know the equivalent ICD-10-AM code, enter it here",
                "   - This will skip AI matching for that row",
                "",
                "2. Hospital ICD Code (Required):",
                "   - Your facility's internal ICD code",
                "   - Must be unique within your template",
                "",
                "3. Diagnosis Name (Required):",
                "   - The medical condition name",
                "   - Be as specific as possible for better AI matching",
                "",
                "4. Diagnosis Description (Optional):",
                "   - Additional details about the diagnosis",
                "   - Helps improve AI matching accuracy",
                "",
                "Important Notes:",
                "- Required columns are highlighted in yellow in the template",
                "- Delete the sample data before adding your own codes",
                "- Save the file as .xlsx format",
                "- Maximum file size: 10MB",
                "- Maximum rows: 5000"
            };

            for (int i = 0; i < instructions.Length; i++)
            {
                instructionsSheet.Cell(i + 2, 1).Value = instructions[i];
                if (instructions[i].StartsWith("Column Descriptions:") || instructions[i].StartsWith("Important Notes:"))
                {
                    instructionsSheet.Cell(i + 2, 1).Style.Font.Bold = true;
                    instructionsSheet.Cell(i + 2, 1).Style.Font.FontSize = 14;
                }
            }

            // Auto-fit columns
            worksheet.Columns().AdjustToContents();
            instructionsSheet.Columns().AdjustToContents();

            // Save to memory stream
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public byte[] GenerateServiceCodesTemplate()
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Service Codes Template");

            var headers = new[]
            {
                "ItemId",
                "ItemRelation",
                "Name",
                "NPHIESCode",
                "NPHIESDescription"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                var cell = worksheet.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.LightBlue;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            var sampleData = new[]
            {
                new { ItemId = "LAB1001", ItemRelation = "HB_A1C", Name = "Hemoglobin A1C", NphiesCode = "", NphiesDescription = "" },
                new { ItemId = "RAD2005", ItemRelation = "CHEST_XRAY", Name = "Chest X-Ray PA", NphiesCode = "TEST", NphiesDescription = "TEST" },
                new { ItemId = "TEST", ItemRelation = "CONSULT_GEN", Name = "RINOFED TAB", NphiesCode = "TEST", NphiesDescription = "TEST" },
                new { ItemId = "MED3007", ItemRelation = "AMOX500", Name = "ALGESAL BAUME CREAM", NphiesCode = "XYZ123", NphiesDescription = "Example code prefilled" }
            };

            for (int i = 0; i < sampleData.Length; i++)
            {
                var row = i + 2;
                worksheet.Cell(row, 1).Value = sampleData[i].ItemId;
                worksheet.Cell(row, 2).Value = sampleData[i].ItemRelation;
                worksheet.Cell(row, 3).Value = sampleData[i].Name;
                worksheet.Cell(row, 4).Value = sampleData[i].NphiesCode;
                worksheet.Cell(row, 5).Value = sampleData[i].NphiesDescription;

                for (int col = 1; col <= 5; col++)
                {
                    worksheet.Cell(row, col).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    if (col == 2)
                        worksheet.Cell(row, col).Style.Fill.BackgroundColor = XLColor.LightYellow; // required
                }
            }

            var instructionsSheet = workbook.Worksheets.Add("Instructions");
            instructionsSheet.Cell(1, 1).Value = "Service Codes Mapping Template Instructions";
            instructionsSheet.Cell(1, 1).Style.Font.Bold = true;
            instructionsSheet.Cell(1, 1).Style.Font.FontSize = 16;

            var lines = new[]
            {
                "Columns:",
                "1) ItemId: Optional provider item identifier",
                "2) ItemRelation: Required - unique key per item within your system (used for mapping)",
                "3) Name: Recommended - used for AI suggestions",
                "4) NPHIESCode: Optional - prefill to skip AI",
                "5) NPHIESDescription: Optional - extra context",
                "",
                "Tips:",
                "- Provide clear 'Name' for better AI suggestions.",
                "- If 'NPHIESCode' is known, the row will be considered mapped.",
                "- Leave NPHIESCode empty to get AI suggestions on the mapping page."
            };

            for (int i = 0; i < lines.Length; i++)
            {
                instructionsSheet.Cell(i + 2, 1).Value = lines[i];
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public ExcelValidationResult ValidateTemplate(IFormFile file)
        {
            var result = new ExcelValidationResult();

            try
            {
                // Check file size (10MB limit)
                if (file.Length > 10 * 1024 * 1024)
                {
                    result.Errors.Add("File size exceeds 10MB limit");
                    return result;
                }

                // Check file extension
                var extension = Path.GetExtension(file.FileName).ToLower();
                if (extension != ".xlsx" && extension != ".xls")
                {
                    result.Errors.Add("Only .xlsx and .xls files are supported");
                    return result;
                }

                using var stream = file.OpenReadStream();
                using var workbook = new XLWorkbook(stream);

                var worksheet = workbook.Worksheets.FirstOrDefault();
                if (worksheet == null)
                {
                    result.Errors.Add("No worksheets found in the file");
                    return result;
                }

                // Check headers
                var expectedHeaders = new[] { "ICD-10-AM Code", "Hospital ICD Code", "Diagnosis Name", "Diagnosis Description" };
                var actualHeaders = new string[4];

                for (int i = 1; i <= 4; i++)
                {
                    actualHeaders[i - 1] = worksheet.Cell(1, i).GetValue<string>().Trim();
                }

                for (int i = 0; i < expectedHeaders.Length; i++)
                {
                    if (!string.Equals(expectedHeaders[i], actualHeaders[i], StringComparison.OrdinalIgnoreCase))
                    {
                        result.Errors.Add($"Column {i + 1} should be '{expectedHeaders[i]}' but found '{actualHeaders[i]}'");
                    }
                }

                if (result.Errors.Any())
                {
                    return result;
                }

                // Parse data rows
                var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;
                result.TotalRows = Math.Max(0, lastRow - 1); // Exclude header row

                if (result.TotalRows == 0)
                {
                    result.Errors.Add("No data rows found. Please add your ICD codes to the template.");
                    return result;
                }

                if (result.TotalRows > 5000)
                {
                    result.Errors.Add("Maximum 5000 rows allowed. Please split your data into smaller files.");
                    return result;
                }

                // Validate data rows
                var hospitalCodes = new HashSet<string>();

                for (int row = 2; row <= lastRow; row++)
                {
                    var icdAmCode = worksheet.Cell(row, 1).GetValue<string>().Trim();
                    var hospitalCode = worksheet.Cell(row, 2).GetValue<string>().Trim();
                    var diagnosisName = worksheet.Cell(row, 3).GetValue<string>().Trim();
                    var description = worksheet.Cell(row, 4).GetValue<string>().Trim();

                    // Check required fields
                    if (string.IsNullOrEmpty(hospitalCode))
                    {
                        result.Errors.Add($"Row {row}: Hospital ICD Code is required");
                        continue;
                    }

                    if (string.IsNullOrEmpty(diagnosisName))
                    {
                        result.Errors.Add($"Row {row}: Diagnosis Name is required");
                        continue;
                    }

                    // Check for duplicate hospital codes
                    if (hospitalCodes.Contains(hospitalCode))
                    {
                        result.Errors.Add($"Row {row}: Duplicate Hospital ICD Code '{hospitalCode}'");
                        continue;
                    }

                    hospitalCodes.Add(hospitalCode);

                    // Add valid row to results
                    result.ValidRows.Add(new ExcelIcdImportDto
                    {
                        Icd10AmCode = string.IsNullOrEmpty(icdAmCode) ? null : icdAmCode,
                        HospitalCode = hospitalCode,
                        DiagnosisName = diagnosisName,
                        DiagnosisDescription = string.IsNullOrEmpty(description) ? null : description
                    });
                }

                result.ValidRowCount = result.ValidRows.Count;
                result.ErrorRowCount = result.TotalRows - result.ValidRowCount;
                result.IsValid = !result.Errors.Any() && result.ValidRowCount > 0;

                return result;
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Error reading file: {ex.Message}");
                return result;
            }
        }
    }

    public class ExcelValidationResult
    {
        public bool IsValid { get; set; }
        public int TotalRows { get; set; }
        public int ValidRowCount { get; set; }
        public int ErrorRowCount { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<ExcelIcdImportDto> ValidRows { get; set; } = new();
    }
}