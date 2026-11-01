using System;
using System.Linq;
using NUnit.Framework;

namespace CrisisVR.Tests
{
    public class LogicTests
    {
        private static readonly DateTime Inicio = new DateTime(2026, 10, 15, 10, 15, 30);
        private static SessionRecorder Nueva() => new SessionRecorder("CVR-20261015-101530-A3F9", Inicio, "1.0");

        [Test]
        public void CodigoDeSesionTieneFechaYHexadecimalInvariantes()
        {
            string id = SessionIdFactory.Create(Inicio, new Random(1));
            Assert.That(id, Is.EqualTo("CVR-20261015-101530-" + new Random(1).Next(0, 0x10000).ToString("X4", System.Globalization.CultureInfo.InvariantCulture)));
            Assert.That(SessionIdFactory.IsValid(id), Is.True);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("CVR-20261015-101530-a3f9")]
        [TestCase("CVR-2026101-101530-A3F9")]
        [TestCase("CVR-20261015-101530-A3FG")]
        [TestCase("CVR-20261015-101530-A3F9\n")]
        public void CodigoInvalidoSeRechaza(string id) => Assert.That(SessionIdFactory.IsValid(id), Is.False);

        [Test]
        public void TemporizadorRespetaPausaYExpiraUnaSolaVez()
        {
            var timer = new DecisionTimer(45);
            timer.Start();
            timer.SetPaused(true);
            Assert.That(timer.Tick(60), Is.False);
            Assert.That(timer.Remaining, Is.EqualTo(45));
            timer.SetPaused(false);
            Assert.That(timer.Expired, Is.False);
            Assert.That(timer.Tick(-10), Is.False);
            Assert.That(timer.Remaining, Is.EqualTo(45));
            Assert.That(timer.Tick(44), Is.False);
            Assert.That(timer.Tick(2), Is.True);
            Assert.That(timer.Remaining, Is.Zero);
            Assert.That(timer.Running, Is.False);
            Assert.That(timer.Expired, Is.True);
            Assert.That(timer.Tick(60), Is.False);
            timer.Start();
            Assert.That(timer.Expired, Is.False);
            timer.Stop();
            Assert.That(timer.Tick(60), Is.False);
            Assert.That(timer.Expired, Is.False);
            Assert.That(timer.Remaining, Is.EqualTo(45));
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        [TestCase(float.NaN)]
        public void DuracionInvalidaLanza(float duration) => Assert.Throws<ArgumentOutOfRangeException>(() => new DecisionTimer(duration));

        [Test]
        public void RegistraUnaVezYNumeraEventosSinHora()
        {
            var r = Nueva();
            Assert.That(r.Data.startedAt, Is.EqualTo("2026-10-15 10:15"));
            Assert.That(r.RecordAct1(true), Is.True);
            Assert.That(r.RecordAct1(false), Is.False);
            Assert.That(r.Data.Act1Ruta, Is.EqualTo("Corregir"));
            r.CompleteAct1();
            r.CompleteAct1();
            Assert.That(r.RecordAct2(2), Is.True);
            Assert.That(r.RecordAct2(1), Is.False);
            r.CompleteAct2();
            Assert.That(r.RecordAct3(4), Is.True);
            Assert.That(r.RecordAct3(1), Is.False);
            r.CompleteAct3();
            r.CompleteAct3();
            r.Interrupt();
            Assert.That(r.Data.eventLog, Is.EqualTo(new[] { "01 ACTO1_ACCION_CORREGIR", "02 ACTO1_COMPLETADO", "03 ACTO2_PLAN_ORIGINAL", "04 ACTO2_COMPLETADO", "05 ACTO3_OPCION_D", "06 ACTO3_COMPLETADO", "07 SESION_COMPLETA" }));
            Assert.That(r.Data.endReason, Is.EqualTo("Completa"));
            Assert.That(r.Data.sessionCompleted && r.IsFinished, Is.True);
            Assert.That(r.RecordAct1(false) || r.RecordAct2(3) || r.RecordAct3(2), Is.False);
        }

        [Test]
        public void CompletarSinRegistroLanzaYLasOpcionesSeValidan()
        {
            var r = Nueva();
            Assert.Throws<InvalidOperationException>(() => r.CompleteAct1());
            Assert.Throws<InvalidOperationException>(() => r.CompleteAct2());
            Assert.Throws<InvalidOperationException>(() => r.CompleteAct3());
            foreach (int value in new[] { -1, 0, 4 }) Assert.Throws<ArgumentOutOfRangeException>(() => r.RecordAct2(value));
            foreach (int value in new[] { -1, 0, 5 }) Assert.Throws<ArgumentOutOfRangeException>(() => r.RecordAct3(value));
            Assert.That(r.Data.eventLog, Is.Empty);
        }

        [Test]
        public void InterrumpirEsIdempotenteYBloqueaRegistros()
        {
            var r = Nueva();
            r.Interrupt();
            r.Interrupt();
            Assert.That(r.RecordAct1(true), Is.False);
            Assert.That(r.RecordAct2(1), Is.False);
            Assert.That(r.RecordAct3(1), Is.False);
            Assert.That(r.Data.eventLog, Is.EqualTo(new[] { "01 SESION_INTERRUMPIDA" }));
            Assert.That(r.Data.endReason, Is.EqualTo("Interrumpida"));
            Assert.That(r.IsFinished, Is.True);
        }

        [Test]
        public void NumeracionContinuaDespuesDeNoventaYNueve()
        {
            var r = Nueva();
            for (int i = 1; i < 100; i++) r.LogEvent("PRUEBA");
            Assert.That(r.LogEvent("PRUEBA"), Is.EqualTo("100 PRUEBA"));
            Assert.That(Nueva().LogEvent("PRUEBA"), Is.EqualTo("01 PRUEBA"));
        }

        [Test, Combinatorial]
        public void ReporteCompletoCubreLasVeinticuatroCombinaciones(
            [Values(false, true)] bool retry, [Values(1, 2, 3)] int adapt, [Values(1, 2, 3, 4)] int team)
        {
            var r = Nueva();
            r.RecordAct1(retry); r.CompleteAct1();
            r.RecordAct2(adapt); r.CompleteAct2();
            r.RecordAct3(team); r.CompleteAct3();
            string report = ReportBuilder.Build(r.Data);
            string[] adaptTexts = { "seleccionó el plan nuevo (práctica virtual).", "seleccionó el plan original (práctica presencial).", "ante el aviso, el estudiante no seleccionó ninguna opción durante el tiempo disponible, y la sesión continuó." };
            string[] teamTexts = {
                "Propongo que cada uno diga qué parte puede entregar y para cuándo, y armamos un plan juntos.",
                "Lucía, yo puedo ayudarte con la presentación para avanzar más rápido.",
                "Mateo no ha avanzado nada; sigamos sin él y repartamos su parte entre nosotros.",
                "Prefiero esperar a ver qué deciden ustedes." };
            Assert.That(report, Does.Contain(retry ? "seleccionó la opción de corregir el proyecto." : "tras recibir la retroalimentación, el estudiante no seleccionó ninguna acción durante el tiempo disponible, y la sesión continuó."));
            Assert.That(report, Does.Contain(adaptTexts[adapt - 1]));
            Assert.That(report, Does.Contain("«" + teamTexts[team - 1] + "»"));
            Assert.That(report, Does.Contain("Estado de la sesión: Completa"));
            Assert.That(report.Split('\n').Count(line => line.EndsWith("completada: sí")), Is.EqualTo(3));
            Assert.That(report, Does.Not.Contain("Sin registro"));
            Assert.That(report, Does.Not.Contain("\r"));
            foreach (string entry in r.Data.eventLog) Assert.That(report, Does.Not.Contain(entry));
        }

        [Test]
        public void ReporteInterrumpidoEnSituacionDosYCamposNulos()
        {
            var r = Nueva();
            r.RecordAct1(false); r.CompleteAct1();
            Assert.That(r.Data.Act1Ruta, Is.EqualTo("NoActuo"));
            r.RecordAct2(1); r.Interrupt();
            string report = ReportBuilder.Build(r.Data);
            Assert.That(report, Does.Contain("Estado de la sesión: Interrumpida"));
            Assert.That(report, Does.Contain("SITUACIÓN 3 — Reunión de equipo\nSituación diseñada en relación con: trabajo en equipo (marco del proyecto; no se mide).\nSin registro: la sesión terminó antes de esta situación."));
            Assert.That(report, Does.Contain("Situación 2 completada: no"));
            foreach (string entry in r.Data.eventLog) Assert.That(report, Does.Not.Contain(entry));
            Assert.That(ReportBuilder.FileName(r.Data.sessionId), Is.EqualTo("CrisisVR_CVR-20261015-101530-A3F9.txt"));
            var empty = new SessionData { endReason = null };
            Assert.That(ReportBuilder.Build(empty), Does.Contain("Código de sesión: \nFecha y hora de inicio: \nVersión de la aplicación: \nEstado de la sesión: \n"));
        }

        [TestCase(1, "A")]
        [TestCase(2, "B")]
        [TestCase(3, "C")]
        [TestCase(4, "D")]
        public void LetrasYTarjetas(int code, string letter)
        {
            Assert.That(DecisionCodes.TeamLetter(code), Is.EqualTo(letter));
            Assert.That(ReportTexts.EntryCard(code), Is.EqualTo("Situación " + code + " de 3"));
            Assert.That(ReportTexts.ExitCard(code), Is.EqualTo("Fin de la situación " + code + " de 3"));
        }
    }
}