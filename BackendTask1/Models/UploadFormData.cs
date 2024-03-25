using BackendTask1.Controllers;
using Microsoft.AspNetCore.Components;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using BackendTask1.Enums;

public class UploadFormData
{
    public IFormFile? File { get; set; }
    public string? FileName { get; set; }
    public string? Owner { get; set; }
    public string? Description { get; set; }
    public string? CreationDate { get; set; }
    public string? ModificationDate { get; set; }
    public QueryType QueryType { get; set; }
}
