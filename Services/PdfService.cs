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

            string nombreCompleto = $"{dp?.academiccontrol_preinscription_personaldata_paternalSurname} {dp?.academiccontrol_preinscription_personaldata_maternalSurname} {dp?.academiccontrol_preinscription_personaldata_name}".Trim().ToUpper();
            string domicilio = $"{dom?.academiccontrol_preinscription_address_street} #{dom?.academiccontrol_preinscription_address_exteriorNumber} — {dom?.academiccontrol_preinscription_address_neighborhood}, C.P. {dom?.academiccontrol_preinscription_address_zipCode}";
            string edad = dp?.academiccontrol_preinscription_personaldata_birthDate != null
                ? ((int)Math.Floor((DateTime.Now - dp.academiccontrol_preinscription_personaldata_birthDate).TotalDays / 365.25)).ToString()
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

                col.Item().PaddingTop(10).Column(info =>
                {
                    info.Item().Row(r =>
                    {
                        r.RelativeItem().Text(t =>
                        {
                            t.Span("NÚMERO DE FOLIO:  ").Bold();
                            t.Span(e.academiccontrol_preinscription_folio ?? "—");
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
                            t.Span(e.academiccontrol_preinscription_careerRequested.ToUpper());
                        });
                    });
                });

                col.Item().PaddingVertical(4).LineHorizontal(1).LineColor(Colors.Black);
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
                        t.Span(dp?.academiccontrol_preinscription_personaldata_gender?.ToUpper() ?? "—");
                    });
                    r.RelativeItem().Text(t =>
                    {
                        t.Span("Fecha de Nacimiento:  ").Bold();
                        t.Span(dp?.academiccontrol_preinscription_personaldata_birthDate.ToString("dd/MM/yyyy") ?? "—");
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
                        t.Span(dp?.academiccontrol_preinscription_personaldata_CURP ?? "—");
                    });
                    r.RelativeItem().Text(t =>
                    {
                        t.Span("Estado Civil:  ").Bold();
                        t.Span(dp?.academiccontrol_preinscription_personaldata_maritalStatus ?? "—");
                    });
                });

                col.Item().Text(t =>
                {
                    t.Span("Secundaria de procedencia:  ").Bold();
                    t.Span(esc?.academiccontrol_preinscription_academic_originSchool ?? "—");
                });

                col.Item().Row(r =>
                {
                    r.RelativeItem().Text(t =>
                    {
                        t.Span("Egreso secundaria:  ").Bold();
                        t.Span(esc?.academiccontrol_preinscription_academic_endDate?.ToString("yyyy") ?? "—");
                    });
                    r.RelativeItem().Text(t =>
                    {
                        t.Span("Promedio:  ").Bold();
                        t.Span(e.academiccontrol_preinscription_average.ToString("F1"));
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
                        t.Span(dom?.academiccontrol_preinscription_address_municipality?.ToUpper() ?? "—");
                    });
                    r.RelativeItem().Text(t =>
                    {
                        t.Span("Estado:  ").Bold();
                        t.Span(dom?.academiccontrol_preinscription_address_state?.ToUpper() ?? "—");
                    });
                });

                col.Item().Text(t =>
                {
                    t.Span("Correo:  ").Bold();
                    t.Span(dp?.academiccontrol_preinscription_personaldata_email ?? "—");
                });

                col.Item().PaddingVertical(4).LineHorizontal(1).LineColor(Colors.Black);
                col.Item().Text("DATOS DEL TUTOR/PADRE DE FAMILIA").Bold().FontSize(11);

                col.Item().Row(r =>
                {
                    r.RelativeItem().Text(t =>
                    {
                        t.Span("Nombre:  ").Bold();
                        t.Span(tutor?.academiccontrol_preinscription_tutor_fullName?.ToUpper() ?? "—");
                    });
                    r.RelativeItem().Text(t =>
                    {
                        t.Span("Parentesco:  ").Bold();
                        t.Span(tutor?.academiccontrol_preinscription_tutor_relationship ?? "—");
                    });
                    r.RelativeItem().Text(t =>
                    {
                        t.Span("Teléfono:  ").Bold();
                        t.Span(tutor?.academiccontrol_preinscription_tutor_phone ?? "—");
                    });
                });

                col.Item().PaddingVertical(4).LineHorizontal(1).LineColor(Colors.Black);

                col.Item().Row(r =>
                {
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

                col.Item().PaddingTop(8).Background(Colors.Grey.Lighten3).Padding(6).Text(
                    "En caso de no coincidir tu CURP con acta de nacimiento y/o certificado de secundaria, " +
                    "háganos saber su situación para brindarle la atención necesaria.")
                    .FontSize(8).Italic();
            });
        }

        //  INSCRIPCION PDF 
        public byte[] GenerarFichaInscripcion(InscripcionEntity e)
        {
            var dp = e.Preinscripcion?.DatosPersonales;
            var dom = e.Preinscripcion?.Domicilio;
            var esc = e.Preinscripcion?.DatosEscolares;
            var tutor = e.Preinscripcion?.Tutor;

            string nombreCompleto = $"{dp?.academiccontrol_preinscription_personaldata_paternalSurname} {dp?.academiccontrol_preinscription_personaldata_maternalSurname} {dp?.academiccontrol_preinscription_personaldata_name}".Trim().ToUpper();
            string domicilio = $"{dom?.academiccontrol_preinscription_address_street} #{dom?.academiccontrol_preinscription_address_exteriorNumber} — {dom?.academiccontrol_preinscription_address_neighborhood}, C.P. {dom?.academiccontrol_preinscription_address_zipCode}";
            string edad = dp?.academiccontrol_preinscription_personaldata_birthDate != null
                ? ((int)Math.Floor((DateTime.Now - dp.academiccontrol_preinscription_personaldata_birthDate).TotalDays / 365.25)).ToString()
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

                col.Item().PaddingTop(10).Column(info =>
                {
                    info.Item().Row(r =>
                    {
                        r.RelativeItem().Text(t =>
                        {
                            t.Span("MATRÍCULA:  ").Bold();
                            t.Span(e.academiccontrol_inscription_enrollment ?? "—");
                        });
                    });
                    info.Item().Row(r =>
                    {
                        r.RelativeItem().Text(t =>
                        {
                            t.Span("FOLIO DE PREINSCRIPCIÓN:  ").Bold();
                            t.Span(e.Preinscripcion?.academiccontrol_preinscription_folio ?? "—");
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
                            t.Span(e.academiccontrol_inscription_careerRequested.ToUpper());
                        });
                    });
                });

                col.Item().PaddingVertical(4).LineHorizontal(1).LineColor(Colors.Black);
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
                        t.Span(dp?.academiccontrol_preinscription_personaldata_gender?.ToUpper() ?? "—");
                    });
                    r.RelativeItem().Text(t =>
                    {
                        t.Span("Fecha de Nacimiento:  ").Bold();
                        t.Span(dp?.academiccontrol_preinscription_personaldata_birthDate.ToString("dd/MM/yyyy") ?? "—");
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
                        t.Span(dp?.academiccontrol_preinscription_personaldata_CURP ?? "—");
                    });
                    r.RelativeItem().Text(t =>
                    {
                        t.Span("Estado Civil:  ").Bold();
                        t.Span(dp?.academiccontrol_preinscription_personaldata_maritalStatus ?? "—");
                    });
                });

                col.Item().Text(t =>
                {
                    t.Span("Secundaria de procedencia:  ").Bold();
                    t.Span(esc?.academiccontrol_preinscription_academic_originSchool ?? "—");
                });

                col.Item().Row(r =>
                {
                    r.RelativeItem().Text(t =>
                    {
                        t.Span("Egreso secundaria:  ").Bold();
                        t.Span(esc?.academiccontrol_preinscription_academic_endDate?.ToString("yyyy") ?? "—");
                    });
                    r.RelativeItem().Text(t =>
                    {
                        t.Span("Promedio:  ").Bold();
                        t.Span(e.Preinscripcion?.academiccontrol_preinscription_average.ToString("F1") ?? "—");
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
                        t.Span(dom?.academiccontrol_preinscription_address_municipality?.ToUpper() ?? "—");
                    });
                    r.RelativeItem().Text(t =>
                    {
                        t.Span("Estado:  ").Bold();
                        t.Span(dom?.academiccontrol_preinscription_address_state?.ToUpper() ?? "—");
                    });
                });

                col.Item().Text(t =>
                {
                    t.Span("Correo:  ").Bold();
                    t.Span(dp?.academiccontrol_preinscription_personaldata_email ?? "—");
                });

                col.Item().PaddingVertical(4).LineHorizontal(1).LineColor(Colors.Black);
                col.Item().Text("DATOS DEL TUTOR/PADRE DE FAMILIA").Bold().FontSize(11);

                col.Item().Row(r =>
                {
                    r.RelativeItem().Text(t =>
                    {
                        t.Span("Nombre:  ").Bold();
                        t.Span(tutor?.academiccontrol_preinscription_tutor_fullName?.ToUpper() ?? "—");
                    });
                    r.RelativeItem().Text(t =>
                    {
                        t.Span("Parentesco:  ").Bold();
                        t.Span(tutor?.academiccontrol_preinscription_tutor_relationship ?? "—");
                    });
                    r.RelativeItem().Text(t =>
                    {
                        t.Span("Teléfono:  ").Bold();
                        t.Span(tutor?.academiccontrol_preinscription_tutor_phone ?? "—");
                    });
                });

                col.Item().PaddingVertical(4).LineHorizontal(1).LineColor(Colors.Black);
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

                col.Item().PaddingTop(8).Background(Colors.Grey.Lighten3).Padding(6).Text(
                    "En caso de no coincidir tu CURP con acta de nacimiento y/o certificado de estudios, " +
                    "háganos saber su situación para brindarle la atención necesaria.")
                    .FontSize(8).Italic();
            });
        }
    }
}