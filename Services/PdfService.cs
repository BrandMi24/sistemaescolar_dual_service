// Services/PdfService.cs
using ControlEscolar.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ControlEscolar.Services
{
    public class PdfService : IPdfService
    {
        public byte[] GenerarFichaPreinscripcion(PreinscripcionEntity e)
        {
            var dp = e.DatosPersonales;
            var dom = e.Domicilio;
            var esc = e.DatosEscolares;
            var tutor = e.Tutor;

            string nombreCompleto = $"{dp?.ApellidoPaterno} {dp?.ApellidoMaterno} {dp?.Nombre}".Trim().ToUpper();
            string domicilio = $"{dom?.Calle} #{dom?.NumeroExterior} — {dom?.Colonia}, C.P. {dom?.CodigoPostal}";
            string edad = dp?.FechaNacimiento != null
                ? ((int)Math.Floor((DateTime.Now - dp.FechaNacimiento).TotalDays / 365.25)).ToString()
                : "—";

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.Letter);
                    page.Margin(40);
                    page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(10));

                    page.Header().Element(ComposeHeader);
                    page.Content().Element(content => ComposeContent(content, e, dp, dom, esc, tutor, nombreCompleto, domicilio, edad));
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("COMPROMISOS: Ser estudiante regular de secundaria, respetar el grupo y turno asignado y mantener buena conducta.")
                         .FontSize(7).Italic();
                    });
                });
            }).GeneratePdf();
        }

        private void ComposeHeader(IContainer container)
        {
            container.Row(row =>
            {
                // Logo placeholder
                row.ConstantItem(80).Height(60).Border(1).BorderColor(Colors.Grey.Lighten2)
                   .AlignCenter().AlignMiddle()
                   .Text("LOGO").FontSize(10).Bold();

                row.RelativeItem().PaddingLeft(10).Column(col =>
                {
                    col.Item().Text("FICHA DE PREINSCRIPCIÓN")
                       .FontSize(18).Bold().FontColor(Colors.Black);
                    col.Item().Text("GENERACIÓN 2026-2029")
                       .FontSize(11).Bold().FontColor(Colors.Grey.Darken2);
                });
            });
        }

        private void ComposeContent(
            IContainer container,
            PreinscripcionEntity e,
            PreinscripcionDatosPersonalesEntity? dp,
            PreinscripcionDomicilioEntity? dom,
            PreinscripcionEscolarEntity? esc,
            PreinscripcionTutorEntity? tutor,
            string nombreCompleto,
            string domicilio,
            string edad)
        {
            container.Column(col =>
            {
                col.Spacing(8);

                // Folio, nombre y carrera
                col.Item().PaddingTop(10).Column(info =>
                {
                    info.Item().Row(r =>
                    {
                        r.RelativeItem().Text(t =>
                        {
                            t.Span("NÚMERO DE FOLIO:  ").Bold();
                            t.Span(e.Folio ?? "—");
                        });
                    });
                    info.Item().Row(r =>
                    {
                        r.RelativeItem().Text(t =>
                        {
                            t.Span("NOMBRE DEL ASPIRANTE:  ").Bold();
                            t.Span(nombreCompleto);
                        });
                    });
                    info.Item().Row(r =>
                    {
                        r.RelativeItem().Text(t =>
                        {
                            t.Span("CARRERA:  ").Bold();
                            t.Span(e.CarreraSolicitada.ToUpper());
                        });
                    });
                });

                // Separador
                col.Item().PaddingVertical(4).LineHorizontal(1).LineColor(Colors.Black);

                // Sección datos del aspirante
                col.Item().AlignCenter().Text("DATOS DEL ASPIRANTE").Bold().FontSize(11);

                col.Item().PaddingTop(4).Text(t =>
                {
                    t.Span("Nombre:  ").Bold();
                    t.Span(nombreCompleto);
                });

                col.Item().Row(r =>
                {
                    r.RelativeItem().Text(t =>
                    {
                        t.Span("Género:  ").Bold();
                        t.Span(dp?.Sexo?.ToUpper() ?? "—");
                    });
                    r.RelativeItem().Text(t =>
                    {
                        t.Span("Fecha de Nacimiento:  ").Bold();
                        t.Span(dp?.FechaNacimiento.ToString("dd/MM/yyyy") ?? "—");
                    });
                    r.ConstantItem(80).Text(t =>
                    {
                        t.Span("Edad:  ").Bold();
                        t.Span(edad);
                    });
                });

                col.Item().Row(r =>
                {
                    r.RelativeItem().Text(t =>
                    {
                        t.Span("CURP:  ").Bold();
                        t.Span(dp?.CURP ?? "—");
                    });
                    r.RelativeItem().Text(t =>
                    {
                        t.Span("Estado Civil:  ").Bold();
                        t.Span(dp?.EstadoCivil ?? "—");
                    });
                });

                col.Item().Text(t =>
                {
                    t.Span("Secundaria de procedencia:  ").Bold();
                    t.Span(esc?.EscuelaProcedencia ?? "—");
                });

                col.Item().Row(r =>
                {
                    r.RelativeItem().Text(t =>
                    {
                        t.Span("Egreso secundaria:  ").Bold();
                        t.Span(esc?.FinBachillerato?.ToString("yyyy") ?? "—");
                    });
                    r.RelativeItem().Text(t =>
                    {
                        t.Span("Promedio:  ").Bold();
                        t.Span(e.Promedio.ToString("F1"));
                    });
                });

                col.Item().Text(t =>
                {
                    t.Span("Domicilio:  ").Bold();
                    t.Span(domicilio.ToUpper());
                });

                col.Item().Row(r =>
                {
                    r.RelativeItem().Text(t =>
                    {
                        t.Span("Ciudad:  ").Bold();
                        t.Span(dom?.Municipio?.ToUpper() ?? "—");
                    });
                    r.RelativeItem().Text(t =>
                    {
                        t.Span("Estado:  ").Bold();
                        t.Span(dom?.Estado?.ToUpper() ?? "—");
                    });
                });

                col.Item().Text(t =>
                {
                    t.Span("Correo:  ").Bold();
                    t.Span(dp?.Email ?? "—");
                });

                // Separador
                col.Item().PaddingVertical(4).LineHorizontal(1).LineColor(Colors.Black);

                // Datos del tutor
                col.Item().Text("DATOS DEL TUTOR/PADRE DE FAMILIA").Bold().FontSize(11);

                col.Item().Row(r =>
                {
                    r.RelativeItem().Text(t =>
                    {
                        t.Span("Nombre:  ").Bold();
                        t.Span(tutor?.TutorNombre?.ToUpper() ?? "—");
                    });
                    r.RelativeItem().Text(t =>
                    {
                        t.Span("Parentesco:  ").Bold();
                        t.Span(tutor?.Parentesco ?? "—");
                    });
                    r.RelativeItem().Text(t =>
                    {
                        t.Span("Teléfono:  ").Bold();
                        t.Span(tutor?.Telefono ?? "—");
                    });
                });

                // Separador
                col.Item().PaddingVertical(4).LineHorizontal(1).LineColor(Colors.Black);

                // Documentos y compromiso
                col.Item().Row(r =>
                {
                    // Columna izquierda: documentos
                    r.RelativeItem().Column(docs =>
                    {
                        docs.Item().Text("DOCUMENTOS A ENTREGAR:").Bold();
                        docs.Spacing(3);
                        foreach (var doc in new[]
                        {
                            "Acta de nacimiento actualizada",
                            "CURP actualizado",
                            "Certificado de secundaria",
                            "Comprobante de domicilio (3 meses)",
                            "6 fotografías tamaño infantil papel mate",
                            "Carta de buena conducta",
                            "Pago de colegiatura"
                        })
                        {
                            docs.Item().Text($"• {doc}").FontSize(9);
                        }
                    });

                    // Columna derecha: compromiso
                    r.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(8).Column(comp =>
                    {
                        comp.Item().AlignCenter().Text("SOLO EN CASO DE FALTA DE CERTIFICADO DE SECUNDARIA")
                            .Bold().FontSize(8);
                        comp.Item().PaddingTop(4).Text("Razón por la que no lo tiene:").FontSize(8).Italic();
                        comp.Item().PaddingTop(20).LineHorizontal(1).LineColor(Colors.Black);
                        comp.Item().PaddingTop(20).AlignCenter()
                            .Text("NOMBRE Y FIRMA COMPROMISO PADRE Y/O TUTOR").FontSize(8).Bold();
                    });
                });

                // Nota final
                col.Item().PaddingTop(8).Background(Colors.Grey.Lighten3).Padding(6).Text(
                    "En caso de no coincidir tu CURP con acta de nacimiento y/o certificado de secundaria, " +
                    "háganos saber su situación para brindarle la atención necesaria.")
                    .FontSize(8).Italic();
            });
        }



        // INSCRIPCION PDF


        public byte[] GenerarFichaInscripcion(InscripcionEntity e)
        {
            var dp = e.Preinscripcion?.DatosPersonales;
            var dom = e.Preinscripcion?.Domicilio;
            var esc = e.Preinscripcion?.DatosEscolares;
            var tutor = e.Preinscripcion?.Tutor;

            string nombreCompleto = $"{dp?.ApellidoPaterno} {dp?.ApellidoMaterno} {dp?.Nombre}".Trim().ToUpper();
            string domicilio = $"{dom?.Calle} #{dom?.NumeroExterior} — {dom?.Colonia}, C.P. {dom?.CodigoPostal}";
            string edad = dp?.FechaNacimiento != null
                ? ((int)Math.Floor((DateTime.Now - dp.FechaNacimiento).TotalDays / 365.25)).ToString()
                : "—";

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.Letter);
                    page.Margin(40);
                    page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(10));

                    page.Header().Element(ComposeHeaderInscripcion);
                    page.Content().Element(content => ComposeContentInscripcion(content, e, dp, dom, esc, tutor, nombreCompleto, domicilio, edad));
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("COMPROMISOS: Respetar el reglamento institucional, mantener buena conducta y cumplir con los requisitos académicos.")
                         .FontSize(7).Italic();
                    });
                });
            }).GeneratePdf();
        }

        private void ComposeHeaderInscripcion(IContainer container)
        {
            container.Row(row =>
            {
                row.ConstantItem(80).Height(60).Border(1).BorderColor(Colors.Grey.Lighten2)
                   .AlignCenter().AlignMiddle()
                   .Text("LOGO").FontSize(10).Bold();

                row.RelativeItem().PaddingLeft(10).Column(col =>
                {
                    col.Item().Text("FICHA DE INSCRIPCIÓN")
                       .FontSize(18).Bold().FontColor(Colors.Black);
                    col.Item().Text("GENERACIÓN 2026-2029")
                       .FontSize(11).Bold().FontColor(Colors.Grey.Darken2);
                });
            });
        }

        private void ComposeContentInscripcion(
            IContainer container,
            InscripcionEntity e,
            PreinscripcionDatosPersonalesEntity? dp,
            PreinscripcionDomicilioEntity? dom,
            PreinscripcionEscolarEntity? esc,
            PreinscripcionTutorEntity? tutor,
            string nombreCompleto,
            string domicilio,
            string edad)
        {
            container.Column(col =>
            {
                col.Spacing(8);

                // Matrícula, nombre y carrera
                col.Item().PaddingTop(10).Column(info =>
                {
                    info.Item().Row(r =>
                    {
                        r.RelativeItem().Text(t =>
                        {
                            t.Span("MATRÍCULA:  ").Bold();
                            t.Span(e.Matricula ?? "—");
                        });
                    });
                    info.Item().Row(r =>
                    {
                        r.RelativeItem().Text(t =>
                        {
                            t.Span("FOLIO DE PREINSCRIPCIÓN:  ").Bold();
                            t.Span(e.Preinscripcion?.Folio ?? "—");
                        });
                    });
                    info.Item().Row(r =>
                    {
                        r.RelativeItem().Text(t =>
                        {
                            t.Span("NOMBRE DEL ASPIRANTE:  ").Bold();
                            t.Span(nombreCompleto);
                        });
                    });
                    info.Item().Row(r =>
                    {
                        r.RelativeItem().Text(t =>
                        {
                            t.Span("CARRERA:  ").Bold();
                            t.Span(e.CarreraSolicitada.ToUpper());
                        });
                    });
                });

                // Separador
                col.Item().PaddingVertical(4).LineHorizontal(1).LineColor(Colors.Black);

                // Datos del aspirante
                col.Item().AlignCenter().Text("DATOS DEL ASPIRANTE").Bold().FontSize(11);

                col.Item().PaddingTop(4).Text(t =>
                {
                    t.Span("Nombre:  ").Bold();
                    t.Span(nombreCompleto);
                });

                col.Item().Row(r =>
                {
                    r.RelativeItem().Text(t =>
                    {
                        t.Span("Género:  ").Bold();
                        t.Span(dp?.Sexo?.ToUpper() ?? "—");
                    });
                    r.RelativeItem().Text(t =>
                    {
                        t.Span("Fecha de Nacimiento:  ").Bold();
                        t.Span(dp?.FechaNacimiento.ToString("dd/MM/yyyy") ?? "—");
                    });
                    r.ConstantItem(80).Text(t =>
                    {
                        t.Span("Edad:  ").Bold();
                        t.Span(edad);
                    });
                });

                col.Item().Row(r =>
                {
                    r.RelativeItem().Text(t =>
                    {
                        t.Span("CURP:  ").Bold();
                        t.Span(dp?.CURP ?? "—");
                    });
                    r.RelativeItem().Text(t =>
                    {
                        t.Span("Estado Civil:  ").Bold();
                        t.Span(dp?.EstadoCivil ?? "—");
                    });
                });

                col.Item().Text(t =>
                {
                    t.Span("Secundaria de procedencia:  ").Bold();
                    t.Span(esc?.EscuelaProcedencia ?? "—");
                });

                col.Item().Row(r =>
                {
                    r.RelativeItem().Text(t =>
                    {
                        t.Span("Egreso secundaria:  ").Bold();
                        t.Span(esc?.FinBachillerato?.ToString("yyyy") ?? "—");
                    });
                    r.RelativeItem().Text(t =>
                    {
                        t.Span("Promedio:  ").Bold();
                        t.Span(e.Preinscripcion?.Promedio.ToString("F1") ?? "—");
                    });
                });

                col.Item().Text(t =>
                {
                    t.Span("Domicilio:  ").Bold();
                    t.Span(domicilio.ToUpper());
                });

                col.Item().Row(r =>
                {
                    r.RelativeItem().Text(t =>
                    {
                        t.Span("Ciudad:  ").Bold();
                        t.Span(dom?.Municipio?.ToUpper() ?? "—");
                    });
                    r.RelativeItem().Text(t =>
                    {
                        t.Span("Estado:  ").Bold();
                        t.Span(dom?.Estado?.ToUpper() ?? "—");
                    });
                });

                col.Item().Text(t =>
                {
                    t.Span("Correo:  ").Bold();
                    t.Span(dp?.Email ?? "—");
                });

                // Separador
                col.Item().PaddingVertical(4).LineHorizontal(1).LineColor(Colors.Black);

                // Datos del tutor
                col.Item().Text("DATOS DEL TUTOR/PADRE DE FAMILIA").Bold().FontSize(11);

                col.Item().Row(r =>
                {
                    r.RelativeItem().Text(t =>
                    {
                        t.Span("Nombre:  ").Bold();
                        t.Span(tutor?.TutorNombre?.ToUpper() ?? "—");
                    });
                    r.RelativeItem().Text(t =>
                    {
                        t.Span("Parentesco:  ").Bold();
                        t.Span(tutor?.Parentesco ?? "—");
                    });
                    r.RelativeItem().Text(t =>
                    {
                        t.Span("Teléfono:  ").Bold();
                        t.Span(tutor?.Telefono ?? "—");
                    });
                });

                // Separador
                col.Item().PaddingVertical(4).LineHorizontal(1).LineColor(Colors.Black);

                // Documentos entregados
                col.Item().Text("DOCUMENTOS ENTREGADOS:").Bold().FontSize(11);
                col.Spacing(3);
                foreach (var doc in new[]
                {
            "Acta de nacimiento actualizada",
            "CURP actualizado",
            "Certificado de estudios o boleta"
        })
                {
                    col.Item().Text($"• {doc}").FontSize(9);
                }

                // Nota final
                col.Item().PaddingTop(8).Background(Colors.Grey.Lighten3).Padding(6).Text(
                    "En caso de no coincidir tu CURP con acta de nacimiento y/o certificado de estudios, " +
                    "háganos saber su situación para brindarle la atención necesaria.")
                .FontSize(8).Italic();
            });
        }
    }
}