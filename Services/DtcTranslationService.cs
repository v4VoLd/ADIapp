using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ADIapp.Services;

/// <summary>
/// Client-side localization provider for Diagnostic Trouble Codes (DTCs / P-codes) and categories.
/// Provides offline, multi-language descriptions for EN, FR, ES, DE without requiring backend localization.
/// </summary>
public static class DtcTranslationService
{
    private static readonly Dictionary<string, Dictionary<string, string>> Categories = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Catalyst & Exhaust"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "Catalyst & Exhaust",
            ["fr"] = "Catalyseur & Échappement",
            ["es"] = "Catalizador y Escape",
            ["de"] = "Katalysator & Abgassystem"
        },
        ["DPF & Particulate Filter"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "DPF & Particulate Filter",
            ["fr"] = "FAP & Filtre à Particules",
            ["es"] = "Filtro de Partículas (DPF)",
            ["de"] = "DPF & Partikelfilter"
        },
        ["SCR & AdBlue"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "SCR & AdBlue",
            ["fr"] = "SCR & AdBlue",
            ["es"] = "SCR y AdBlue",
            ["de"] = "SCR & AdBlue (Harnstoff)"
        },
        ["EGR"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "EGR",
            ["fr"] = "Vanne EGR",
            ["es"] = "Válvula EGR",
            ["de"] = "AGR-Ventil"
        },
        ["EVAP"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "EVAP",
            ["fr"] = "Système EVAP",
            ["es"] = "Sistema EVAP",
            ["de"] = "EVAP-Kraftstoffdampfsystem"
        },
        ["Oxygen Sensors"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "Oxygen Sensors",
            ["fr"] = "Sondes Lambda / Oxygène",
            ["es"] = "Sensores de Oxígeno / Lambda",
            ["de"] = "Lambdasonden / O2-Sensoren"
        },
        ["Intake / Swirl Flaps"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "Intake / Swirl Flaps",
            ["fr"] = "Admission / Volets de turbulence",
            ["es"] = "Admisión / Trampillas Swirl",
            ["de"] = "Ansaugtrakt / Drallklappen"
        },
        ["Boost / Turbocharger"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "Boost / Turbocharger",
            ["fr"] = "Pression de suralimentation / Turbo",
            ["es"] = "Presión de Sobrealimentación / Turbo",
            ["de"] = "Ladedruck / Turbolader"
        },
        ["Fuel System"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "Fuel System",
            ["fr"] = "Système de carburant / Rampe",
            ["es"] = "Sistema de Combustible / Riel",
            ["de"] = "Kraftstoffsystem & Common-Rail"
        },
        ["Air Flow Sensors"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "Air Flow Sensors",
            ["fr"] = "Capteurs de débit d'air (Débitmètre/MAP)",
            ["es"] = "Sensores de Flujo de Aire (MAF/MAP)",
            ["de"] = "Luftmassenmesser & Drucksensoren (LMM/MAP)"
        },
        ["Ignition / Misfires"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "Ignition / Misfires",
            ["fr"] = "Allumage / Ratés de combustion",
            ["es"] = "Encendido / Fallos de combustión",
            ["de"] = "Zündung & Verbrennungsaussetzer"
        },
        ["Secondary Air Injection"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "Secondary Air Injection",
            ["fr"] = "Injection d'air secondaire",
            ["es"] = "Inyección de Aire Secundario",
            ["de"] = "Sekundärluftsystem"
        },
        ["Speed & Idle"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "Speed & Idle",
            ["fr"] = "Vitesse véhicule & Ralenti",
            ["es"] = "Velocidad del Vehículo y Ralentí",
            ["de"] = "Geschwindigkeit & Leerlauf"
        },
        ["Control Module"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "Control Module",
            ["fr"] = "Calculateur moteur (ECU/PCM)",
            ["es"] = "Módulo de Control del Motor (ECU/PCM)",
            ["de"] = "Motorsteuergerät (ECU/PCM)"
        }
    };

    private static readonly Dictionary<string, Dictionary<string, string>> CodeDescriptions = new(StringComparer.OrdinalIgnoreCase)
    {
        // Catalyst & Exhaust
        ["P0420"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "Catalyst System Efficiency Below Threshold (Bank 1)",
            ["fr"] = "Efficacité du système de catalyseur inférieure au seuil (Ligne 1)",
            ["es"] = "Eficiencia del sistema catalizador por debajo del umbral (Banco 1)",
            ["de"] = "Wirkungsgrad Katalysatorsystem unter Schwellenwert (Bank 1)"
        },
        ["P0421"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "Warm Up Catalyst Efficiency Below Threshold (Bank 1)",
            ["fr"] = "Efficacité du pré-catalyseur inférieure au seuil (Ligne 1)",
            ["es"] = "Eficiencia del catalizador de calentamiento por debajo del umbral (Banco 1)",
            ["de"] = "Wirkungsgrad Vorkatalysator unter Schwellenwert (Bank 1)"
        },
        ["P0430"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "Catalyst System Efficiency Below Threshold (Bank 2)",
            ["fr"] = "Efficacité du système catalytique inférieure au seuil (Ligne 2)",
            ["es"] = "Eficiencia del sistema catalizador por debajo del umbral (Banco 2)",
            ["de"] = "Wirkungsgrad Katalysatorsystem unter Schwellenwert (Bank 2)"
        },
        ["P0431"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "Warm Up Catalyst Efficiency Below Threshold (Bank 2)",
            ["fr"] = "Efficacité du pré-catalyseur inférieure au seuil (Ligne 2)",
            ["es"] = "Eficiencia del catalizador de calentamiento por debajo del umbral (Banco 2)",
            ["de"] = "Wirkungsgrad Vorkatalysator unter Schwellenwert (Bank 2)"
        },

        // DPF / Particulate Filter
        ["P2002"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "Diesel Particulate Filter Efficiency Below Threshold (Bank 1)",
            ["fr"] = "Efficacité du filtre à particules diesel inférieure au seuil (Ligne 1)",
            ["es"] = "Eficiencia del filtro de partículas diésel por debajo del umbral (Banco 1)",
            ["de"] = "Wirkungsgrad Dieselpartikelfilter unter Schwellenwert (Bank 1)"
        },
        ["P2003"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "Diesel Particulate Filter Efficiency Below Threshold (Bank 2)",
            ["fr"] = "Efficacité du filtre à particules diesel inférieure au seuil (Ligne 2)",
            ["es"] = "Eficiencia del filtro de partículas diésel por debajo del umbral (Banco 2)",
            ["de"] = "Wirkungsgrad Dieselpartikelfilter unter Schwellenwert (Bank 2)"
        },
        ["P242F"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "Diesel Particulate Filter Restriction - Ash Accumulation",
            ["fr"] = "Restriction du filtre à particules - Accumulation de cendres",
            ["es"] = "Restricción del filtro de partículas diésel - Acumulación de ceniza",
            ["de"] = "Partikelfilter blockiert - Ascheansammlung"
        },
        ["P244A"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "Diesel Particulate Filter Differential Pressure Too Low",
            ["fr"] = "Pression différentielle du filtre à particules trop basse",
            ["es"] = "Presión diferencial del filtro de partículas demasiado baja",
            ["de"] = "Differenzdruck Dieselpartikelfilter zu niedrig"
        },
        ["P244B"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "Diesel Particulate Filter Differential Pressure Too High",
            ["fr"] = "Pression différentielle du filtre à particules trop haute",
            ["es"] = "Presión diferencial del filtro de partículas demasiado alta",
            ["de"] = "Differenzdruck Dieselpartikelfilter zu hoch"
        },
        ["P2452"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "Diesel Particulate Filter Pressure Sensor A Circuit",
            ["fr"] = "Circuit du capteur de pression A du filtre à particules",
            ["es"] = "Circuito del sensor de presión A del filtro de partículas",
            ["de"] = "Stromkreis Drucksensor A Dieselpartikelfilter"
        },
        ["P2453"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "Diesel Particulate Filter Pressure Sensor A Circuit Range/Performance",
            ["fr"] = "Plage/performance du capteur de pression A du filtre à particules",
            ["es"] = "Rango/rendimiento del sensor de presión A del filtro de partículas",
            ["de"] = "Bereich/Funktion Drucksensor A Dieselpartikelfilter"
        },
        ["P2454"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "Diesel Particulate Filter Pressure Sensor A Circuit Low",
            ["fr"] = "Signal bas du capteur de pression A du filtre à particules",
            ["es"] = "Señal baja del circuito del sensor de presión A del filtro de partículas",
            ["de"] = "Signal zu niedrig Drucksensor A Dieselpartikelfilter"
        },
        ["P2455"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "Diesel Particulate Filter Pressure Sensor A Circuit High",
            ["fr"] = "Signal haut du capteur de pression A du filtre à particules",
            ["es"] = "Señal alta del circuito del sensor de presión A del filtro de partículas",
            ["de"] = "Signal zu hoch Drucksensor A Dieselpartikelfilter"
        },
        ["P2458"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "Diesel Particulate Filter Regeneration Duration",
            ["fr"] = "Durée de régénération du filtre à particules diesel",
            ["es"] = "Duración de regeneración del filtro de partículas diésel",
            ["de"] = "Regenerationsdauer Dieselpartikelfilter fehlerhaft"
        },
        ["P2459"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "Diesel Particulate Filter Regeneration Frequency",
            ["fr"] = "Fréquence de régénération du filtre à particules diesel",
            ["es"] = "Frecuencia de regeneración del filtro de partículas diésel",
            ["de"] = "Regenerationshäufigkeit Dieselpartikelfilter fehlerhaft"
        },
        ["P2463"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "Diesel Particulate Filter Restriction - Soot Accumulation",
            ["fr"] = "Colmatage du filtre à particules - Accumulation de suie",
            ["es"] = "Restricción del filtro de partículas - Acumulación de hollín",
            ["de"] = "Partikelfilter verstopft - Rußansammlung"
        },

        // SCR & AdBlue / NOx
        ["P204F"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "Reductant System Performance (Bank 1)",
            ["fr"] = "Performance du système de réactif (Ligne 1)",
            ["es"] = "Rendimiento del sistema de agente reductor (Banco 1)",
            ["de"] = "Leistung Reduktionsmittelsystem (Bank 1)"
        },
        ["P20EE"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "SCR NOx Catalyst Efficiency Below Threshold (Bank 1)",
            ["fr"] = "Efficacité du catalyseur SCR NOx inférieure au seuil (Ligne 1)",
            ["es"] = "Eficiencia del catalizador SCR NOx por debajo del umbral (Banco 1)",
            ["de"] = "Wirkungsgrad SCR-NOx-Katalysator unter Schwellenwert (Bank 1)"
        },
        ["P20EF"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "SCR NOx Catalyst Efficiency Below Threshold (Bank 2)",
            ["fr"] = "Efficacité du catalyseur SCR NOx inférieure au seuil (Ligne 2)",
            ["es"] = "Eficiencia del catalizador SCR NOx por debajo del umbral (Banco 2)",
            ["de"] = "Wirkungsgrad SCR-NOx-Katalysator unter Schwellenwert (Bank 2)"
        },
        ["P20BA"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "Reductant Heater A Control Circuit Performance",
            ["fr"] = "Performance du circuit du réchauffeur de réactif A",
            ["es"] = "Rendimiento del circuito de control del calentador de reductor A",
            ["de"] = "Leistung Steuerstromkreis Reduktionsmittelheizung A"
        },
        ["P20BB"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "Reductant Heater A Control Circuit Low",
            ["fr"] = "Signal bas du circuit de commande du réchauffeur de réactif A",
            ["es"] = "Circuito de control del calentador de reductor A bajo",
            ["de"] = "Signal Steuerstromkreis Reduktionsmittelheizung A zu niedrig"
        },
        ["P20BC"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "Reductant Heater A Control Circuit High",
            ["fr"] = "Signal haut du circuit de commande du réchauffeur de réactif A",
            ["es"] = "Circuito de control del calentador de reductor A alto",
            ["de"] = "Signal Steuerstromkreis Reduktionsmittelheizung A zu hoch"
        },
        ["P2047"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "Reductant Injection Valve Circuit/Open (Bank 1 Unit 1)",
            ["fr"] = "Circuit d'injecteur de réactif ouvert (Ligne 1 Unité 1)",
            ["es"] = "Circuito de válvula de inyección de reductor abierto (Banco 1 Unidad 1)",
            ["de"] = "Stromkreis Dosierventil Reduktionsmittel unterbrochen (Bank 1 Einheit 1)"
        },
        ["P2048"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "Reductant Injection Valve Circuit Low (Bank 1 Unit 1)",
            ["fr"] = "Signal bas du circuit de commande de l'injecteur de réactif (Ligne 1 Unité 1)",
            ["es"] = "Circuito de válvula de inyección de reductor bajo (Banco 1 Unidad 1)",
            ["de"] = "Signal Dosierventil Reduktionsmittel zu niedrig (Bank 1 Einheit 1)"
        },
        ["P2200"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "NOx Sensor Circuit (Bank 1 Sensor 1)",
            ["fr"] = "Circuit du capteur de NOx (Ligne 1 Capteur 1)",
            ["es"] = "Circuito del sensor de NOx (Banco 1 Sensor 1)",
            ["de"] = "Stromkreis NOx-Sensor (Bank 1 Sensor 1)"
        },
        ["P2201"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "NOx Sensor Circuit Range/Performance (Bank 1 Sensor 1)",
            ["fr"] = "Plage/performance du capteur de NOx (Ligne 1 Capteur 1)",
            ["es"] = "Rango/rendimiento del circuito del sensor de NOx (Banco 1 Sensor 1)",
            ["de"] = "Bereich/Funktion Stromkreis NOx-Sensor (Bank 1 Sensor 1)"
        },
        ["P2202"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "NOx Sensor Circuit Low Input (Bank 1 Sensor 1)",
            ["fr"] = "Signal d'entrée bas du capteur de NOx (Ligne 1 Capteur 1)",
            ["es"] = "Entrada baja del circuito del sensor de NOx (Banco 1 Sensor 1)",
            ["de"] = "Eingangssignal Stromkreis NOx-Sensor zu niedrig (Bank 1 Sensor 1)"
        },
        ["P2209"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "NOx Sensor Heater Sense Circuit Range/Performance",
            ["fr"] = "Plage/performance du circuit de chauffage du capteur NOx",
            ["es"] = "Rango/rendimiento del circuito calefactor del sensor NOx",
            ["de"] = "Bereich/Funktion Stromkreis NOx-Sensor-Heizung"
        },
        ["P229F"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "NOx Sensor Circuit Range/Performance (Bank 1 Sensor 2)",
            ["fr"] = "Plage/performance du capteur de NOx (Ligne 1 Capteur 2)",
            ["es"] = "Rango/rendimiento del circuito del sensor de NOx (Banco 1 Sensor 2)",
            ["de"] = "Bereich/Funktion Stromkreis NOx-Sensor (Bank 1 Sensor 2)"
        },

        // EGR
        ["P0400"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "Exhaust Gas Recirculation Flow Malfunction",
            ["fr"] = "Dysfonctionnement du débit de recyclage des gaz d'échappement (EGR)",
            ["es"] = "Fallo en el flujo de recirculación de gases de escape (EGR)",
            ["de"] = "Fehlfunktion Abgasrückführungsdurchsatz (AGR)"
        },
        ["P0401"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "Exhaust Gas Recirculation Flow Insufficient Detected",
            ["fr"] = "Débit insuffisant détecté pour la vanne EGR",
            ["es"] = "Flujo insuficiente detectado en recirculación de gases (EGR)",
            ["de"] = "Unzureichender Abgasrückführungsdurchsatz erkannt (AGR)"
        },
        ["P0402"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "Exhaust Gas Recirculation Flow Excessive Detected",
            ["fr"] = "Débit excessif détecté pour la vanne EGR",
            ["es"] = "Flujo excesivo detectado en recirculación de gases (EGR)",
            ["de"] = "Übermäßiger Abgasrückführungsdurchsatz erkannt (AGR)"
        },
        ["P0403"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "Exhaust Gas Recirculation Circuit Malfunction",
            ["fr"] = "Dysfonctionnement du circuit de commande de la vanne EGR",
            ["es"] = "Fallo en el circuito de control de la válvula EGR",
            ["de"] = "Fehlfunktion Steuerstromkreis Abgasrückführung (AGR)"
        },
        ["P0404"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "Exhaust Gas Recirculation Circuit Range/Performance",
            ["fr"] = "Plage/performance du circuit de la vanne EGR",
            ["es"] = "Rango/rendimiento del circuito de recirculación de gases (EGR)",
            ["de"] = "Bereich/Funktion Steuerstromkreis Abgasrückführung (AGR)"
        },
        ["P0405"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "Exhaust Gas Recirculation Sensor A Circuit Low",
            ["fr"] = "Signal d'entrée bas du capteur A de position EGR",
            ["es"] = "Señal baja en el circuito del sensor A de posición EGR",
            ["de"] = "Signal Sensor A Abgasrückführung zu niedrig"
        },
        ["P0406"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "Exhaust Gas Recirculation Sensor A Circuit High",
            ["fr"] = "Signal d'entrée haut du capteur A de position EGR",
            ["es"] = "Señal alta en el circuito del sensor A de posición EGR",
            ["de"] = "Signal Sensor A Abgasrückführung zu hoch"
        },
        ["P0409"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "Exhaust Gas Recirculation Sensor A Circuit",
            ["fr"] = "Circuit du capteur A de position de la vanne EGR",
            ["es"] = "Circuito del sensor A de recirculación de gases (EGR)",
            ["de"] = "Stromkreis Sensor A Abgasrückführung"
        },

        // EVAP
        ["P0440"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "Evaporative Emission Control System Malfunction",
            ["fr"] = "Dysfonctionnement du système de purge canister (EVAP)",
            ["es"] = "Fallo en el sistema de control de emisiones por evaporación (EVAP)",
            ["de"] = "Fehlfunktion Tankentlüftungssystem (EVAP)"
        },
        ["P0441"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "Evaporative Emission Control System Incorrect Purge Flow",
            ["fr"] = "Débit de purge incorrect du système EVAP",
            ["es"] = "Flujo de purga incorrecto en el sistema EVAP",
            ["de"] = "Fehlerhafter Spülfluss Tankentlüftungssystem"
        },
        ["P0442"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "Evaporative Emission Control System Leak Detected (Small Leak)",
            ["fr"] = "Petite fuite détectée dans le système EVAP",
            ["es"] = "Fuga pequeña detectada en el sistema EVAP",
            ["de"] = "Geringes Leck im Tankentlüftungssystem erkannt"
        },
        ["P0443"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "Evaporative Emission Control System Purge Control Valve Circuit",
            ["fr"] = "Circuit de l'électrovanne de purge canister (EVAP)",
            ["es"] = "Circuito de la válvula de control de purga EVAP",
            ["de"] = "Stromkreis Spülventil Tankentlüftungssystem"
        },
        ["P0455"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "Evaporative Emission Control System Leak Detected (Gross Leak)",
            ["fr"] = "Fuite importante détectée dans le système EVAP",
            ["es"] = "Fuga grande detectada en el sistema EVAP",
            ["de"] = "Großes Leck im Tankentlüftungssystem erkannt"
        },
        ["P0456"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "Evaporative Emission Control System Leak Detected (Very Small Leak)",
            ["fr"] = "Très petite fuite détectée dans le système EVAP",
            ["es"] = "Fuga muy pequeña detectada en el sistema EVAP",
            ["de"] = "Sehr kleines Leck im Tankentlüftungssystem erkannt"
        },

        // Oxygen (O2) Sensors
        ["P0130"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "O2 Sensor Circuit Malfunction (Bank 1 Sensor 1)",
            ["fr"] = "Dysfonctionnement du circuit de la sonde lambda (Ligne 1 Capteur 1)",
            ["es"] = "Fallo en el circuito de la sonda lambda (Banco 1 Sensor 1)",
            ["de"] = "Fehlfunktion Stromkreis Lambdasonde (Bank 1 Sensor 1)"
        },
        ["P0131"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "O2 Sensor Circuit Low Voltage (Bank 1 Sensor 1)",
            ["fr"] = "Tension basse de la sonde lambda (Ligne 1 Capteur 1)",
            ["es"] = "Tensión baja en el circuito de la sonda lambda (Banco 1 Sensor 1)",
            ["de"] = "Spannung Stromkreis Lambdasonde zu niedrig (Bank 1 Sensor 1)"
        },
        ["P0132"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "O2 Sensor Circuit High Voltage (Bank 1 Sensor 1)",
            ["fr"] = "Tension haute de la sonde lambda (Ligne 1 Capteur 1)",
            ["es"] = "Tensión alta en el circuito de la sonda lambda (Banco 1 Sensor 1)",
            ["de"] = "Spannung Stromkreis Lambdasonde zu hoch (Bank 1 Sensor 1)"
        },
        ["P0133"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "O2 Sensor Circuit Slow Response (Bank 1 Sensor 1)",
            ["fr"] = "Réponse lente de la sonde lambda (Ligne 1 Capteur 1)",
            ["es"] = "Respuesta lenta de la sonda lambda (Banco 1 Sensor 1)",
            ["de"] = "Langsame Reaktion Lambdasonde (Bank 1 Sensor 1)"
        },
        ["P0134"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "O2 Sensor Circuit No Activity Detected (Bank 1 Sensor 1)",
            ["fr"] = "Aucune activité détectée sur la sonde lambda (Ligne 1 Capteur 1)",
            ["es"] = "Sin actividad detectada en la sonda lambda (Banco 1 Sensor 1)",
            ["de"] = "Keine Aktivität Lambdasonde erkannt (Bank 1 Sensor 1)"
        },
        ["P0135"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "O2 Sensor Heater Circuit Malfunction (Bank 1 Sensor 1)",
            ["fr"] = "Panne du circuit de réchauffage de la sonde lambda (Ligne 1 Capteur 1)",
            ["es"] = "Fallo en el circuito calefactor de la sonda lambda (Banco 1 Sensor 1)",
            ["de"] = "Fehlfunktion Heizstromkreis Lambdasonde (Bank 1 Sensor 1)"
        },
        ["P0136"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "O2 Sensor Circuit Malfunction (Bank 1 Sensor 2)",
            ["fr"] = "Dysfonctionnement du circuit de la sonde lambda (Ligne 1 Capteur 2)",
            ["es"] = "Fallo en el circuito de la sonda lambda (Banco 1 Sensor 2)",
            ["de"] = "Fehlfunktion Stromkreis Lambdasonde (Bank 1 Sensor 2)"
        },
        ["P0137"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "O2 Sensor Circuit Low Voltage (Bank 1 Sensor 2)",
            ["fr"] = "Tension basse de la sonde lambda (Ligne 1 Capteur 2)",
            ["es"] = "Tensión baja en el circuito de la sonda lambda (Banco 1 Sensor 2)",
            ["de"] = "Spannung Stromkreis Lambdasonde zu niedrig (Bank 1 Sensor 2)"
        },
        ["P0138"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "O2 Sensor Circuit High Voltage (Bank 1 Sensor 2)",
            ["fr"] = "Tension haute de la sonde lambda (Ligne 1 Capteur 2)",
            ["es"] = "Tensión alta en el circuito de la sonda lambda (Banco 1 Sensor 2)",
            ["de"] = "Spannung Stromkreis Lambdasonde zu hoch (Bank 1 Sensor 2)"
        },
        ["P0139"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "O2 Sensor Circuit Slow Response (Bank 1 Sensor 2)",
            ["fr"] = "Réponse lente de la sonde lambda (Ligne 1 Capteur 2)",
            ["es"] = "Respuesta lenta de la sonda lambda (Banco 1 Sensor 2)",
            ["de"] = "Langsame Reaktion Lambdasonde (Bank 1 Sensor 2)"
        },
        ["P0140"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "O2 Sensor Circuit No Activity Detected (Bank 1 Sensor 2)",
            ["fr"] = "Aucune activité détectée sur la sonde lambda (Ligne 1 Capteur 2)",
            ["es"] = "Sin actividad detectada en la sonda lambda (Banco 1 Sensor 2)",
            ["de"] = "Keine Aktivität Lambdasonde erkannt (Bank 1 Sensor 2)"
        },
        ["P0141"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "O2 Sensor Heater Circuit Malfunction (Bank 1 Sensor 2)",
            ["fr"] = "Panne du circuit de réchauffage de la sonde lambda (Ligne 1 Capteur 2)",
            ["es"] = "Fallo en el circuito calefactor de la sonda lambda (Banco 1 Sensor 2)",
            ["de"] = "Fehlfunktion Heizstromkreis Lambdasonde (Bank 1 Sensor 2)"
        },

        // Swirl Flaps
        ["P2004"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "Intake Manifold Runner Control Stuck Open (Bank 1)",
            ["fr"] = "Volets de turbulence du collecteur d'admission bloqués ouverts (Ligne 1)",
            ["es"] = "Trampilla del colector de admisión atascada abierta (Banco 1)",
            ["de"] = "Saugrohr-Drallklappe klemmt offen (Bank 1)"
        },
        ["P2006"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "Intake Manifold Runner Control Stuck Closed (Bank 1)",
            ["fr"] = "Volets de turbulence du collecteur d'admission bloqués fermés (Ligne 1)",
            ["es"] = "Trampilla del colector de admisión atascada cerrada (Banco 1)",
            ["de"] = "Saugrohr-Drallklappe klemmt geschlossen (Bank 1)"
        },
        ["P2008"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "Intake Manifold Runner Control Circuit/Open (Bank 1)",
            ["fr"] = "Circuit de commande des volets d'admission ouvert (Ligne 1)",
            ["es"] = "Circuito de control de la trampilla del colector abierto (Banco 1)",
            ["de"] = "Steuerstromkreis Saugrohrklappe unterbrochen (Bank 1)"
        },
        ["P2015"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "Intake Manifold Flap Position Sensor Range/Performance",
            ["fr"] = "Plage/performance du capteur de position des volets d'admission",
            ["es"] = "Rango/rendimiento del sensor de posición de trampillas del colector",
            ["de"] = "Bereich/Funktion Sensor Drallklappenstellung"
        },

        // Boost / Turbocharger
        ["P0234"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "Turbocharger/Supercharger Overboost Condition",
            ["fr"] = "Pression de suralimentation trop élevée (Overboost)",
            ["es"] = "Condición de sobrepresión del turbocompresor (Overboost)",
            ["de"] = "Ladedruck-Überlastzustand Turbolader (Overboost)"
        },
        ["P0235"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "Turbocharger/Supercharger Boost Sensor A Circuit Malfunction",
            ["fr"] = "Panne du circuit du capteur de suralimentation A",
            ["es"] = "Fallo en el circuito del sensor de sobrealimentación A",
            ["de"] = "Fehlfunktion Stromkreis Ladedrucksensor A"
        },
        ["P0236"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "Turbocharger/Supercharger Boost Sensor A Circuit Range/Performance",
            ["fr"] = "Plage/performance du capteur de suralimentation A",
            ["es"] = "Rango/rendimiento del sensor de sobrealimentación A",
            ["de"] = "Bereich/Funktion Stromkreis Ladedrucksensor A"
        },
        ["P0237"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "Turbocharger/Supercharger Boost Sensor A Circuit Low",
            ["fr"] = "Signal bas du capteur de suralimentation A",
            ["es"] = "Señal baja del sensor de sobrealimentación A",
            ["de"] = "Signal Ladedrucksensor A zu niedrig"
        },
        ["P0238"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "Turbocharger/Supercharger Boost Sensor A Circuit High",
            ["fr"] = "Signal haut du capteur de suralimentation A",
            ["es"] = "Señal alta del sensor de sobrealimentación A",
            ["de"] = "Signal Ladedrucksensor A zu hoch"
        },
        ["P0299"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "Turbocharger/Supercharger Underboost",
            ["fr"] = "Pression de suralimentation insuffisante (Underboost)",
            ["es"] = "Presión insuficiente del turbocompresor (Underboost)",
            ["de"] = "Ladedruck zu niedrig Turbolader (Underboost)"
        },
        ["P2563"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "Turbocharger Boost Control Position Sensor Circuit Range/Performance",
            ["fr"] = "Plage/performance du capteur de position du turbo",
            ["es"] = "Rango/rendimiento del sensor de posición del turbocompresor",
            ["de"] = "Bereich/Funktion Positionssensor Ladedruckregelung"
        },

        // Fuel System
        ["P0087"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "Fuel Rail/System Pressure - Too Low",
            ["fr"] = "Pression de la rampe commune de carburant trop basse",
            ["es"] = "Presión del riel de combustible demasiado baja",
            ["de"] = "Kraftstoff-Verteilerrohrdruck zu niedrig"
        },
        ["P0088"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "Fuel Rail/System Pressure - Too High",
            ["fr"] = "Pression de la rampe commune de carburant trop haute",
            ["es"] = "Presión del riel de combustible demasiado alta",
            ["de"] = "Kraftstoff-Verteilerrohrdruck zu hoch"
        },
        ["P0089"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "Fuel Pressure Regulator 1 Performance",
            ["fr"] = "Performance du régulateur de pression de carburant 1",
            ["es"] = "Rendimiento del regulador de presión de combustible 1",
            ["de"] = "Leistung Kraftstoffdruckregler 1"
        },
        ["P0090"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "Fuel Pressure Regulator 1 Control Circuit",
            ["fr"] = "Circuit de commande du régulateur de pression de carburant 1",
            ["es"] = "Circuito de control del regulador de presión de combustible 1",
            ["de"] = "Steuerstromkreis Kraftstoffdruckregler 1"
        },
        ["P2293"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "Fuel Pressure Regulator 2 Performance",
            ["fr"] = "Performance du régulateur de pression de carburant 2",
            ["es"] = "Rendimiento del regulador de presión de combustible 2",
            ["de"] = "Leistung Kraftstoffdruckregler 2"
        },

        // MAF / MAP
        ["P0100"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "Mass or Volume Air Flow Circuit Malfunction",
            ["fr"] = "Panne du circuit du débitmètre d'air (MAF)",
            ["es"] = "Fallo en el circuito del medidor de flujo de aire (MAF)",
            ["de"] = "Fehlfunktion Stromkreis Luftmassenmesser (LMM)"
        },
        ["P0101"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "Mass or Volume Air Flow Circuit Range/Performance",
            ["fr"] = "Plage/performance du circuit du débitmètre d'air (MAF)",
            ["es"] = "Rango/rendimiento del medidor de flujo de aire (MAF)",
            ["de"] = "Bereich/Funktion Stromkreis Luftmassenmesser (LMM)"
        },
        ["P0102"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "Mass or Volume Air Flow Circuit Low Input",
            ["fr"] = "Signal d'entrée bas du débitmètre d'air (MAF)",
            ["es"] = "Señal baja del medidor de flujo de aire (MAF)",
            ["de"] = "Signal Luftmassenmesser zu niedrig (LMM)"
        },
        ["P0103"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "Mass or Volume Air Flow Circuit High Input",
            ["fr"] = "Signal d'entrée haut du débitmètre d'air (MAF)",
            ["es"] = "Señal alta del medidor de flujo de aire (MAF)",
            ["de"] = "Signal Luftmassenmesser zu hoch (LMM)"
        },
        ["P0106"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "Manifold Absolute Pressure/Barometric Pressure Circuit Range/Performance",
            ["fr"] = "Plage/performance du capteur de pression absolue (MAP)",
            ["es"] = "Rango/rendimiento del sensor de presión absoluta del colector (MAP)",
            ["de"] = "Bereich/Funktion Saugrohrdrucksensor/Barometersensor"
        },
        ["P0107"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "Manifold Absolute Pressure/Barometric Pressure Circuit Low Input",
            ["fr"] = "Signal bas du capteur de pression absolue (MAP)",
            ["es"] = "Señal baja del sensor de presión absoluta del colector (MAP)",
            ["de"] = "Signal Saugrohrdrucksensor zu niedrig"
        },
        ["P0108"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "Manifold Absolute Pressure/Barometric Pressure Circuit High Input",
            ["fr"] = "Signal haut du capteur de pression absolue (MAP)",
            ["es"] = "Señal alta del sensor de presión absoluta (MAP)",
            ["de"] = "Signal Saugrohrdrucksensor zu hoch"
        },

        // Misfires
        ["P0300"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "Random/Multiple Cylinder Misfire Detected",
            ["fr"] = "Ratés d'allumage multiples ou aléatoires détectés",
            ["es"] = "Fallos de encendido aleatorios/múltiples detectados",
            ["de"] = "Zufällige/mehrere Verbrennungsaussetzer erkannt"
        },

        // Speed / Idle
        ["P0500"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "Vehicle Speed Sensor Malfunction",
            ["fr"] = "Dysfonctionnement du capteur de vitesse du véhicule",
            ["es"] = "Fallo en el sensor de velocidad del vehículo (VSS)",
            ["de"] = "Fehlfunktion Geschwindigkeitssensor (VSS)"
        },
        ["P0505"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "Idle Air Control System Malfunction",
            ["fr"] = "Dysfonctionnement du régulateur de ralenti",
            ["es"] = "Fallo en el sistema de control del aire de ralentí",
            ["de"] = "Fehlfunktion Leerlaufregelsystem"
        },

        // Control Module
        ["P0600"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "Serial Communication Link Malfunction",
            ["fr"] = "Dysfonctionnement de la liaison de communication série",
            ["es"] = "Fallo en el enlace de comunicación serie",
            ["de"] = "Fehlfunktion serielle Kommunikationsverbindung"
        },
        ["P0601"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "Internal Control Module Memory Check Sum Error",
            ["fr"] = "Erreur de somme de contrôle (checksum) de la mémoire interne",
            ["es"] = "Error de suma de comprobación de memoria del módulo",
            ["de"] = "Prüfsummenfehler interner Speicher Motorsteuergerät"
        },
        ["P0602"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "Control Module Programming Error",
            ["fr"] = "Erreur de programmation du calculateur moteur",
            ["es"] = "Error de programación del módulo de control",
            ["de"] = "Programmierfehler Motorsteuergerät"
        },
        ["P0606"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "ECM/PCM Processor Fault",
            ["fr"] = "Défaut processeur du calculateur moteur (ECM/PCM)",
            ["es"] = "Fallo en el procesador del módulo de control (ECM/PCM)",
            ["de"] = "Prozessorfehler Motorsteuergerät (ECM/PCM)"
        }
    };

    /// <summary>
    /// Translates a DTC fault code description into the specified language (en, fr, es, de).
    /// Falls back to default backend description or code name if unavailable.
    /// </summary>
    public static string GetDescription(string code, string? defaultDescription, string? langCode)
    {
        string lang = NormalizeLang(langCode);

        // 1. Direct code dictionary match
        if (CodeDescriptions.TryGetValue(code, out var translations))
        {
            if (translations.TryGetValue(lang, out var val) && !string.IsNullOrWhiteSpace(val))
                return val;
            if (translations.TryGetValue("en", out var enVal) && !string.IsNullOrWhiteSpace(enVal))
                return enVal;
        }

        // 2. Pattern-based cylinder misfire match (P0301 - P0316)
        if (code.StartsWith("P030", StringComparison.OrdinalIgnoreCase) || code.StartsWith("P031", StringComparison.OrdinalIgnoreCase))
        {
            var match = Regex.Match(code, @"^P03([0-9]{2})$", RegexOptions.IgnoreCase);
            if (match.Success && int.TryParse(match.Groups[1].Value, out int cylNum) && cylNum >= 1 && cylNum <= 16)
            {
                return lang switch
                {
                    "fr" => $"Raté d'allumage détecté dans le cylindre {cylNum}",
                    "es" => $"Fallo de encendido detectado en el cilindro {cylNum}",
                    "de" => $"Verbrennungsaussetzer in Zylinder {cylNum} erkannt",
                    _ => $"Cylinder {cylNum} Misfire Detected"
                };
            }
        }

        // 3. Fallback to backend description if present
        if (!string.IsNullOrWhiteSpace(defaultDescription))
        {
            if (lang == "en") return defaultDescription;
            return TranslateDescription(defaultDescription, lang);
        }

        // 4. Ultimate fallback
        return $"Fault Code {code}";
    }

    private static string TranslateDescription(string desc, string lang)
    {
        string res = desc;
        if (lang == "fr")
        {
            res = Regex.Replace(res, @"\bLost Communication With\b", "Communication perdue avec", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bControl Circuit/Open\b", "Circuit de commande/Ouvert", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bControl Circuit Range/Performance\b", "Plage/Rendement du circuit de commande", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bCircuit Range/Performance\b", "Plage/Rendement du circuit", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bCircuit Low\b", "Circuit bas", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bCircuit High\b", "Circuit haut", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bCircuit Intermittent/Erratic\b", "Circuit intermittent/erratique", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bCircuit Intermittent\b", "Circuit intermittent", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bCircuit/Open\b", "Circuit/Ouvert", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bPerformance/Stuck Off\b", "Performance/Bloqué désactivé", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bPerformance/Stuck On\b", "Performance/Bloqué activé", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bStuck Closed\b", "Bloqué fermé", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bStuck Open\b", "Bloqué ouvert", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bStuck On\b", "Bloqué activé", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bStuck Off\b", "Bloqué désactivé", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bBelow Threshold\b", "inférieure au seuil", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bAbove Threshold\b", "supérieure au seuil", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bEfficiency Below Threshold\b", "Efficacité inférieure au seuil", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bSupply Voltage Circuit\b", "Circuit de tension d'alimentation", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bSupply Voltage Low\b", "Tension d'alimentation basse", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bSupply Voltage High\b", "Tension d'alimentation haute", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bToo High\b", "Trop élevé", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bToo Low\b", "Trop bas", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bSlow Response\b", "Réponse lente", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bNo Signal\b", "Aucun signal", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bNo Activity Detected\b", "Aucune activité détectée", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bBank 1 Sensor 1\b", "Ligne 1 Capteur 1", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bBank 1 Sensor 2\b", "Ligne 1 Capteur 2", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bBank 2 Sensor 1\b", "Ligne 2 Capteur 1", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bBank 2 Sensor 2\b", "Ligne 2 Capteur 2", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bBank 1\b", "Ligne 1", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bBank 2\b", "Ligne 2", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bSensor A\b", "Capteur A", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bSensor B\b", "Capteur B", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bFuel Volume Regulator\b", "Régulateur de débit de carburant", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bFuel Shutoff Valve\b", "Électrovanne d'arrêt de carburant", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bFuel Rail/System Pressure\b", "Pression rampe/système de carburant", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bCatalyst System\b", "Système de catalyseur", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bParticulate Filter\b", "Filtre à particules", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bExhaust Gas Recirculation\b", "Recirculation des gaz d'échappement (EGR)", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bMass or Volume Air Flow\b", "Débitmètre d'air massique ou volumique", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bManifold Absolute Pressure\b", "Pression absolue du collecteur (MAP)", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bHigh Speed CAN Communication Bus\b", "Bus de communication CAN haute vitesse", RegexOptions.IgnoreCase);
        }
        else if (lang == "es")
        {
            res = Regex.Replace(res, @"\bLost Communication With\b", "Comunicación perdida con", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bControl Circuit/Open\b", "Circuito de control/Abierto", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bControl Circuit Range/Performance\b", "Rango/Rendimiento del circuito de control", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bCircuit Range/Performance\b", "Rango/Rendimiento del circuito", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bCircuit Low\b", "Circuito bajo", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bCircuit High\b", "Circuito alto", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bCircuit Intermittent/Erratic\b", "Circuito intermitente/errático", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bCircuit Intermittent\b", "Circuito intermitente", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bCircuit/Open\b", "Circuito/Abierto", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bPerformance/Stuck Off\b", "Rendimiento/Atascado apagado", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bPerformance/Stuck On\b", "Rendimiento/Atascado encendido", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bStuck Closed\b", "Atascado cerrado", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bStuck Open\b", "Atascado abierto", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bStuck On\b", "Atascado encendido", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bStuck Off\b", "Atascado apagado", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bBelow Threshold\b", "por debajo del umbral", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bAbove Threshold\b", "por encima del umbral", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bEfficiency Below Threshold\b", "Eficiencia por debajo del umbral", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bSupply Voltage Circuit\b", "Circuito de tensión de alimentación", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bSupply Voltage Low\b", "Tensión de alimentación baja", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bSupply Voltage High\b", "Tensión de alimentación alta", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bToo High\b", "Demasiado alto", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bToo Low\b", "Demasiado bajo", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bSlow Response\b", "Respuesta lenta", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bNo Signal\b", "Sin señal", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bNo Activity Detected\b", "No se detecta actividad", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bBank 1 Sensor 1\b", "Banco 1 Sensor 1", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bBank 1 Sensor 2\b", "Banco 1 Sensor 2", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bBank 2 Sensor 1\b", "Banco 2 Sensor 1", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bBank 2 Sensor 2\b", "Banco 2 Sensor 2", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bBank 1\b", "Banco 1", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bBank 2\b", "Banco 2", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bSensor A\b", "Sensor A", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bSensor B\b", "Sensor B", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bFuel Volume Regulator\b", "Regulador de volumen de combustible", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bFuel Shutoff Valve\b", "Válvula de corte de combustible", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bFuel Rail/System Pressure\b", "Presión del riel/sistema de combustible", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bCatalyst System\b", "Sistema catalizador", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bParticulate Filter\b", "Filtro de partículas", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bExhaust Gas Recirculation\b", "Recirculación de gases de escape (EGR)", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bMass or Volume Air Flow\b", "Flujo de aire por masa o volumen (MAF)", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bManifold Absolute Pressure\b", "Presión absoluta del colector (MAP)", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bHigh Speed CAN Communication Bus\b", "Bus de comunicación CAN de alta velocidad", RegexOptions.IgnoreCase);
        }
        else if (lang == "de")
        {
            res = Regex.Replace(res, @"\bLost Communication With\b", "Kommunikation verloren mit", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bControl Circuit/Open\b", "Steuerkreis/Offen", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bControl Circuit Range/Performance\b", "Steuerstromkreis Bereich/Leistung", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bCircuit Range/Performance\b", "Stromkreis Bereich/Leistung", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bCircuit Low\b", "Stromkreis niedrig", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bCircuit High\b", "Stromkreis hoch", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bCircuit Intermittent/Erratic\b", "Stromkreis zeitweilig/fehlerhaft", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bCircuit Intermittent\b", "Stromkreis zeitweilig", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bCircuit/Open\b", "Stromkreis/Offen", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bPerformance/Stuck Off\b", "Leistung/Klemmt aus", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bPerformance/Stuck On\b", "Leistung/Klemmt ein", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bStuck Closed\b", "Klemmt geschlossen", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bStuck Open\b", "Klemmt offen", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bStuck On\b", "Klemmt ein", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bStuck Off\b", "Klemmt aus", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bBelow Threshold\b", "unter Schwellenwert", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bAbove Threshold\b", "über Schwellenwert", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bEfficiency Below Threshold\b", "Wirkungsgrad unter Schwellenwert", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bSupply Voltage Circuit\b", "Versorgungsspannungs-Stromkreis", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bSupply Voltage Low\b", "Versorgungsspannung niedrig", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bSupply Voltage High\b", "Versorgungsspannung hoch", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bToo High\b", "Zu hoch", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bToo Low\b", "Zu niedrig", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bSlow Response\b", "Träge Reaktion", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bNo Signal\b", "Kein Signal", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bNo Activity Detected\b", "Keine Aktivität erkannt", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bBank 1 Sensor 1\b", "Bank 1 Sensor 1", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bBank 1 Sensor 2\b", "Bank 1 Sensor 2", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bBank 2 Sensor 1\b", "Bank 2 Sensor 1", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bBank 2 Sensor 2\b", "Bank 2 Sensor 2", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bBank 1\b", "Bank 1", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bBank 2\b", "Bank 2", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bSensor A\b", "Sensor A", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bSensor B\b", "Sensor B", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bFuel Volume Regulator\b", "Kraftstoffmengenregler", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bFuel Shutoff Valve\b", "Kraftstoffabsperrventil", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bFuel Rail/System Pressure\b", "Kraftstoff-Rail-/Systemdruck", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bCatalyst System\b", "Katalysatorsystem", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bParticulate Filter\b", "Partikelfilter", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bExhaust Gas Recirculation\b", "Abgasrückführung (AGR)", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bMass or Volume Air Flow\b", "Luftmassen- oder Luftmengenmesser", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bManifold Absolute Pressure\b", "Saugrohr-Absolutdruck (MAP)", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\bHigh Speed CAN Communication Bus\b", "Highspeed-CAN-Kommunikationsbus", RegexOptions.IgnoreCase);
        }
        return res;
    }

    /// <summary>
    /// Translates a DTC category string into the specified language (en, fr, es, de).
    /// </summary>
    public static string? GetCategory(string? category, string? langCode)
    {
        if (string.IsNullOrWhiteSpace(category))
            return null;

        string lang = NormalizeLang(langCode);

        if (Categories.TryGetValue(category, out var dict))
        {
            if (dict.TryGetValue(lang, out var val) && !string.IsNullOrWhiteSpace(val))
                return val;
            if (dict.TryGetValue("en", out var enVal) && !string.IsNullOrWhiteSpace(enVal))
                return enVal;
        }

        return category;
    }

    private static string NormalizeLang(string? langCode)
    {
        if (string.IsNullOrWhiteSpace(langCode))
            return "en";
        string l = langCode.Trim().ToLowerInvariant();
        return l switch
        {
            "fr" => "fr",
            "es" => "es",
            "de" => "de",
            _ => "en"
        };
    }
}
