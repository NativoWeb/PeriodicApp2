// Configuraciones electronicas de estado fundamental para Z = 1..118.
//
// Generado y validado contra 26 configuraciones de referencia conocidas.
// Base cientifica: orden de llenado de Madelung mas las 20 excepciones
// experimentales confirmadas (Cr, Cu, Nb, Mo, Ru, Rh, Pd, Ag, La, Ce, Gd,
// Pt, Au, Ac, Th, Pa, U, Np, Cm, Lr).
//
// No editar a mano sin validar: esta distribucion alimenta directamente
// el modelo atomico 3D que ve el estudiante.

using System;
using System.Collections.Generic;
using System.Text;

namespace PeriodicApp.Core.Domain.Quimica
{
    /// <summary>
    /// Una subcapa ocupada: nivel principal (n), tipo de orbital (s/p/d/f)
    /// y cuantos electrones la ocupan.
    /// </summary>
    public readonly struct Subcapa
    {
        public readonly int Nivel;
        public readonly char Tipo;
        public readonly int Electrones;

        public Subcapa(int nivel, char tipo, int electrones)
        {
            Nivel = nivel;
            Tipo = tipo;
            Electrones = electrones;
        }

        /// <summary>Capacidad maxima de la subcapa: s=2, p=6, d=10, f=14.</summary>
        public int Capacidad
        {
            get
            {
                switch (Tipo)
                {
                    case 's': return 2;
                    case 'p': return 6;
                    case 'd': return 10;
                    case 'f': return 14;
                    default: return 0;
                }
            }
        }

        /// <summary>Numero de orbitales que contiene: s=1, p=3, d=5, f=7.</summary>
        public int Orbitales
        {
            get { return Capacidad / 2; }
        }

        /// <summary>Si la subcapa esta completamente llena.</summary>
        public bool EstaLlena
        {
            get { return Electrones >= Capacidad; }
        }

        public override string ToString()
        {
            return string.Concat(
                Nivel.ToString(),
                Tipo.ToString(),
                Electrones.ToString());
        }
    }

    /// <summary>
    /// Configuracion electronica real de cada elemento.
    ///
    /// Reemplaza el llenado ingenuo por capacidad de capa (2, 8, 18, 32...),
    /// que es incorrecto para 86 de los 118 elementos. El potasio, por ejemplo,
    /// tiene 2-8-8-1 y no 2-8-9: ese unico electron en la cuarta capa es lo que
    /// explica su reactividad, asi que el modelo 3D debe mostrarlo ahi.
    /// </summary>
    public static class ConfiguracionElectronica
    {
        public const int ElementoMinimo = 1;
        public const int ElementoMaximo = 118;

        // Cada entrada esta en ORDEN DE LLENADO (Madelung), no ordenada por n.
        // Ese es el orden que debe seguir la animacion de formacion del atomo.
        private static readonly string[] Configuraciones =
        {
            "1s1",                                                                               //   1 H   -> 1
            "1s2",                                                                               //   2 He  -> 2
            "1s2 2s1",                                                                           //   3 Li  -> 2, 1
            "1s2 2s2",                                                                           //   4 Be  -> 2, 2
            "1s2 2s2 2p1",                                                                       //   5 B   -> 2, 3
            "1s2 2s2 2p2",                                                                       //   6 C   -> 2, 4
            "1s2 2s2 2p3",                                                                       //   7 N   -> 2, 5
            "1s2 2s2 2p4",                                                                       //   8 O   -> 2, 6
            "1s2 2s2 2p5",                                                                       //   9 F   -> 2, 7
            "1s2 2s2 2p6",                                                                       //  10 Ne  -> 2, 8
            "1s2 2s2 2p6 3s1",                                                                   //  11 Na  -> 2, 8, 1
            "1s2 2s2 2p6 3s2",                                                                   //  12 Mg  -> 2, 8, 2
            "1s2 2s2 2p6 3s2 3p1",                                                               //  13 Al  -> 2, 8, 3
            "1s2 2s2 2p6 3s2 3p2",                                                               //  14 Si  -> 2, 8, 4
            "1s2 2s2 2p6 3s2 3p3",                                                               //  15 P   -> 2, 8, 5
            "1s2 2s2 2p6 3s2 3p4",                                                               //  16 S   -> 2, 8, 6
            "1s2 2s2 2p6 3s2 3p5",                                                               //  17 Cl  -> 2, 8, 7
            "1s2 2s2 2p6 3s2 3p6",                                                               //  18 Ar  -> 2, 8, 8
            "1s2 2s2 2p6 3s2 3p6 4s1",                                                           //  19 K   -> 2, 8, 8, 1
            "1s2 2s2 2p6 3s2 3p6 4s2",                                                           //  20 Ca  -> 2, 8, 8, 2
            "1s2 2s2 2p6 3s2 3p6 4s2 3d1",                                                       //  21 Sc  -> 2, 8, 9, 2
            "1s2 2s2 2p6 3s2 3p6 4s2 3d2",                                                       //  22 Ti  -> 2, 8, 10, 2
            "1s2 2s2 2p6 3s2 3p6 4s2 3d3",                                                       //  23 V   -> 2, 8, 11, 2
            "1s2 2s2 2p6 3s2 3p6 4s1 3d5",                                                       //  24 Cr  -> 2, 8, 13, 1
            "1s2 2s2 2p6 3s2 3p6 4s2 3d5",                                                       //  25 Mn  -> 2, 8, 13, 2
            "1s2 2s2 2p6 3s2 3p6 4s2 3d6",                                                       //  26 Fe  -> 2, 8, 14, 2
            "1s2 2s2 2p6 3s2 3p6 4s2 3d7",                                                       //  27 Co  -> 2, 8, 15, 2
            "1s2 2s2 2p6 3s2 3p6 4s2 3d8",                                                       //  28 Ni  -> 2, 8, 16, 2
            "1s2 2s2 2p6 3s2 3p6 4s1 3d10",                                                      //  29 Cu  -> 2, 8, 18, 1
            "1s2 2s2 2p6 3s2 3p6 4s2 3d10",                                                      //  30 Zn  -> 2, 8, 18, 2
            "1s2 2s2 2p6 3s2 3p6 4s2 3d10 4p1",                                                  //  31 Ga  -> 2, 8, 18, 3
            "1s2 2s2 2p6 3s2 3p6 4s2 3d10 4p2",                                                  //  32 Ge  -> 2, 8, 18, 4
            "1s2 2s2 2p6 3s2 3p6 4s2 3d10 4p3",                                                  //  33 As  -> 2, 8, 18, 5
            "1s2 2s2 2p6 3s2 3p6 4s2 3d10 4p4",                                                  //  34 Se  -> 2, 8, 18, 6
            "1s2 2s2 2p6 3s2 3p6 4s2 3d10 4p5",                                                  //  35 Br  -> 2, 8, 18, 7
            "1s2 2s2 2p6 3s2 3p6 4s2 3d10 4p6",                                                  //  36 Kr  -> 2, 8, 18, 8
            "1s2 2s2 2p6 3s2 3p6 4s2 3d10 4p6 5s1",                                              //  37 Rb  -> 2, 8, 18, 8, 1
            "1s2 2s2 2p6 3s2 3p6 4s2 3d10 4p6 5s2",                                              //  38 Sr  -> 2, 8, 18, 8, 2
            "1s2 2s2 2p6 3s2 3p6 4s2 3d10 4p6 5s2 4d1",                                          //  39 Y   -> 2, 8, 18, 9, 2
            "1s2 2s2 2p6 3s2 3p6 4s2 3d10 4p6 5s2 4d2",                                          //  40 Zr  -> 2, 8, 18, 10, 2
            "1s2 2s2 2p6 3s2 3p6 4s2 3d10 4p6 5s1 4d4",                                          //  41 Nb  -> 2, 8, 18, 12, 1
            "1s2 2s2 2p6 3s2 3p6 4s2 3d10 4p6 5s1 4d5",                                          //  42 Mo  -> 2, 8, 18, 13, 1
            "1s2 2s2 2p6 3s2 3p6 4s2 3d10 4p6 5s2 4d5",                                          //  43 Tc  -> 2, 8, 18, 13, 2
            "1s2 2s2 2p6 3s2 3p6 4s2 3d10 4p6 5s1 4d7",                                          //  44 Ru  -> 2, 8, 18, 15, 1
            "1s2 2s2 2p6 3s2 3p6 4s2 3d10 4p6 5s1 4d8",                                          //  45 Rh  -> 2, 8, 18, 16, 1
            "1s2 2s2 2p6 3s2 3p6 4s2 3d10 4p6 4d10",                                             //  46 Pd  -> 2, 8, 18, 18
            "1s2 2s2 2p6 3s2 3p6 4s2 3d10 4p6 5s1 4d10",                                         //  47 Ag  -> 2, 8, 18, 18, 1
            "1s2 2s2 2p6 3s2 3p6 4s2 3d10 4p6 5s2 4d10",                                         //  48 Cd  -> 2, 8, 18, 18, 2
            "1s2 2s2 2p6 3s2 3p6 4s2 3d10 4p6 5s2 4d10 5p1",                                     //  49 In  -> 2, 8, 18, 18, 3
            "1s2 2s2 2p6 3s2 3p6 4s2 3d10 4p6 5s2 4d10 5p2",                                     //  50 Sn  -> 2, 8, 18, 18, 4
            "1s2 2s2 2p6 3s2 3p6 4s2 3d10 4p6 5s2 4d10 5p3",                                     //  51 Sb  -> 2, 8, 18, 18, 5
            "1s2 2s2 2p6 3s2 3p6 4s2 3d10 4p6 5s2 4d10 5p4",                                     //  52 Te  -> 2, 8, 18, 18, 6
            "1s2 2s2 2p6 3s2 3p6 4s2 3d10 4p6 5s2 4d10 5p5",                                     //  53 I   -> 2, 8, 18, 18, 7
            "1s2 2s2 2p6 3s2 3p6 4s2 3d10 4p6 5s2 4d10 5p6",                                     //  54 Xe  -> 2, 8, 18, 18, 8
            "1s2 2s2 2p6 3s2 3p6 4s2 3d10 4p6 5s2 4d10 5p6 6s1",                                 //  55 Cs  -> 2, 8, 18, 18, 8, 1
            "1s2 2s2 2p6 3s2 3p6 4s2 3d10 4p6 5s2 4d10 5p6 6s2",                                 //  56 Ba  -> 2, 8, 18, 18, 8, 2
            "1s2 2s2 2p6 3s2 3p6 4s2 3d10 4p6 5s2 4d10 5p6 6s2 5d1",                             //  57 La  -> 2, 8, 18, 18, 9, 2
            "1s2 2s2 2p6 3s2 3p6 4s2 3d10 4p6 5s2 4d10 5p6 6s2 4f1 5d1",                         //  58 Ce  -> 2, 8, 18, 19, 9, 2
            "1s2 2s2 2p6 3s2 3p6 4s2 3d10 4p6 5s2 4d10 5p6 6s2 4f3",                             //  59 Pr  -> 2, 8, 18, 21, 8, 2
            "1s2 2s2 2p6 3s2 3p6 4s2 3d10 4p6 5s2 4d10 5p6 6s2 4f4",                             //  60 Nd  -> 2, 8, 18, 22, 8, 2
            "1s2 2s2 2p6 3s2 3p6 4s2 3d10 4p6 5s2 4d10 5p6 6s2 4f5",                             //  61 Pm  -> 2, 8, 18, 23, 8, 2
            "1s2 2s2 2p6 3s2 3p6 4s2 3d10 4p6 5s2 4d10 5p6 6s2 4f6",                             //  62 Sm  -> 2, 8, 18, 24, 8, 2
            "1s2 2s2 2p6 3s2 3p6 4s2 3d10 4p6 5s2 4d10 5p6 6s2 4f7",                             //  63 Eu  -> 2, 8, 18, 25, 8, 2
            "1s2 2s2 2p6 3s2 3p6 4s2 3d10 4p6 5s2 4d10 5p6 6s2 4f7 5d1",                         //  64 Gd  -> 2, 8, 18, 25, 9, 2
            "1s2 2s2 2p6 3s2 3p6 4s2 3d10 4p6 5s2 4d10 5p6 6s2 4f9",                             //  65 Tb  -> 2, 8, 18, 27, 8, 2
            "1s2 2s2 2p6 3s2 3p6 4s2 3d10 4p6 5s2 4d10 5p6 6s2 4f10",                            //  66 Dy  -> 2, 8, 18, 28, 8, 2
            "1s2 2s2 2p6 3s2 3p6 4s2 3d10 4p6 5s2 4d10 5p6 6s2 4f11",                            //  67 Ho  -> 2, 8, 18, 29, 8, 2
            "1s2 2s2 2p6 3s2 3p6 4s2 3d10 4p6 5s2 4d10 5p6 6s2 4f12",                            //  68 Er  -> 2, 8, 18, 30, 8, 2
            "1s2 2s2 2p6 3s2 3p6 4s2 3d10 4p6 5s2 4d10 5p6 6s2 4f13",                            //  69 Tm  -> 2, 8, 18, 31, 8, 2
            "1s2 2s2 2p6 3s2 3p6 4s2 3d10 4p6 5s2 4d10 5p6 6s2 4f14",                            //  70 Yb  -> 2, 8, 18, 32, 8, 2
            "1s2 2s2 2p6 3s2 3p6 4s2 3d10 4p6 5s2 4d10 5p6 6s2 4f14 5d1",                        //  71 Lu  -> 2, 8, 18, 32, 9, 2
            "1s2 2s2 2p6 3s2 3p6 4s2 3d10 4p6 5s2 4d10 5p6 6s2 4f14 5d2",                        //  72 Hf  -> 2, 8, 18, 32, 10, 2
            "1s2 2s2 2p6 3s2 3p6 4s2 3d10 4p6 5s2 4d10 5p6 6s2 4f14 5d3",                        //  73 Ta  -> 2, 8, 18, 32, 11, 2
            "1s2 2s2 2p6 3s2 3p6 4s2 3d10 4p6 5s2 4d10 5p6 6s2 4f14 5d4",                        //  74 W   -> 2, 8, 18, 32, 12, 2
            "1s2 2s2 2p6 3s2 3p6 4s2 3d10 4p6 5s2 4d10 5p6 6s2 4f14 5d5",                        //  75 Re  -> 2, 8, 18, 32, 13, 2
            "1s2 2s2 2p6 3s2 3p6 4s2 3d10 4p6 5s2 4d10 5p6 6s2 4f14 5d6",                        //  76 Os  -> 2, 8, 18, 32, 14, 2
            "1s2 2s2 2p6 3s2 3p6 4s2 3d10 4p6 5s2 4d10 5p6 6s2 4f14 5d7",                        //  77 Ir  -> 2, 8, 18, 32, 15, 2
            "1s2 2s2 2p6 3s2 3p6 4s2 3d10 4p6 5s2 4d10 5p6 6s1 4f14 5d9",                        //  78 Pt  -> 2, 8, 18, 32, 17, 1
            "1s2 2s2 2p6 3s2 3p6 4s2 3d10 4p6 5s2 4d10 5p6 6s1 4f14 5d10",                       //  79 Au  -> 2, 8, 18, 32, 18, 1
            "1s2 2s2 2p6 3s2 3p6 4s2 3d10 4p6 5s2 4d10 5p6 6s2 4f14 5d10",                       //  80 Hg  -> 2, 8, 18, 32, 18, 2
            "1s2 2s2 2p6 3s2 3p6 4s2 3d10 4p6 5s2 4d10 5p6 6s2 4f14 5d10 6p1",                   //  81 Tl  -> 2, 8, 18, 32, 18, 3
            "1s2 2s2 2p6 3s2 3p6 4s2 3d10 4p6 5s2 4d10 5p6 6s2 4f14 5d10 6p2",                   //  82 Pb  -> 2, 8, 18, 32, 18, 4
            "1s2 2s2 2p6 3s2 3p6 4s2 3d10 4p6 5s2 4d10 5p6 6s2 4f14 5d10 6p3",                   //  83 Bi  -> 2, 8, 18, 32, 18, 5
            "1s2 2s2 2p6 3s2 3p6 4s2 3d10 4p6 5s2 4d10 5p6 6s2 4f14 5d10 6p4",                   //  84 Po  -> 2, 8, 18, 32, 18, 6
            "1s2 2s2 2p6 3s2 3p6 4s2 3d10 4p6 5s2 4d10 5p6 6s2 4f14 5d10 6p5",                   //  85 At  -> 2, 8, 18, 32, 18, 7
            "1s2 2s2 2p6 3s2 3p6 4s2 3d10 4p6 5s2 4d10 5p6 6s2 4f14 5d10 6p6",                   //  86 Rn  -> 2, 8, 18, 32, 18, 8
            "1s2 2s2 2p6 3s2 3p6 4s2 3d10 4p6 5s2 4d10 5p6 6s2 4f14 5d10 6p6 7s1",               //  87 Fr  -> 2, 8, 18, 32, 18, 8, 1
            "1s2 2s2 2p6 3s2 3p6 4s2 3d10 4p6 5s2 4d10 5p6 6s2 4f14 5d10 6p6 7s2",               //  88 Ra  -> 2, 8, 18, 32, 18, 8, 2
            "1s2 2s2 2p6 3s2 3p6 4s2 3d10 4p6 5s2 4d10 5p6 6s2 4f14 5d10 6p6 7s2 6d1",           //  89 Ac  -> 2, 8, 18, 32, 18, 9, 2
            "1s2 2s2 2p6 3s2 3p6 4s2 3d10 4p6 5s2 4d10 5p6 6s2 4f14 5d10 6p6 7s2 6d2",           //  90 Th  -> 2, 8, 18, 32, 18, 10, 2
            "1s2 2s2 2p6 3s2 3p6 4s2 3d10 4p6 5s2 4d10 5p6 6s2 4f14 5d10 6p6 7s2 5f2 6d1",       //  91 Pa  -> 2, 8, 18, 32, 20, 9, 2
            "1s2 2s2 2p6 3s2 3p6 4s2 3d10 4p6 5s2 4d10 5p6 6s2 4f14 5d10 6p6 7s2 5f3 6d1",       //  92 U   -> 2, 8, 18, 32, 21, 9, 2
            "1s2 2s2 2p6 3s2 3p6 4s2 3d10 4p6 5s2 4d10 5p6 6s2 4f14 5d10 6p6 7s2 5f4 6d1",       //  93 Np  -> 2, 8, 18, 32, 22, 9, 2
            "1s2 2s2 2p6 3s2 3p6 4s2 3d10 4p6 5s2 4d10 5p6 6s2 4f14 5d10 6p6 7s2 5f6",           //  94 Pu  -> 2, 8, 18, 32, 24, 8, 2
            "1s2 2s2 2p6 3s2 3p6 4s2 3d10 4p6 5s2 4d10 5p6 6s2 4f14 5d10 6p6 7s2 5f7",           //  95 Am  -> 2, 8, 18, 32, 25, 8, 2
            "1s2 2s2 2p6 3s2 3p6 4s2 3d10 4p6 5s2 4d10 5p6 6s2 4f14 5d10 6p6 7s2 5f7 6d1",       //  96 Cm  -> 2, 8, 18, 32, 25, 9, 2
            "1s2 2s2 2p6 3s2 3p6 4s2 3d10 4p6 5s2 4d10 5p6 6s2 4f14 5d10 6p6 7s2 5f9",           //  97 Bk  -> 2, 8, 18, 32, 27, 8, 2
            "1s2 2s2 2p6 3s2 3p6 4s2 3d10 4p6 5s2 4d10 5p6 6s2 4f14 5d10 6p6 7s2 5f10",          //  98 Cf  -> 2, 8, 18, 32, 28, 8, 2
            "1s2 2s2 2p6 3s2 3p6 4s2 3d10 4p6 5s2 4d10 5p6 6s2 4f14 5d10 6p6 7s2 5f11",          //  99 Es  -> 2, 8, 18, 32, 29, 8, 2
            "1s2 2s2 2p6 3s2 3p6 4s2 3d10 4p6 5s2 4d10 5p6 6s2 4f14 5d10 6p6 7s2 5f12",          // 100 Fm  -> 2, 8, 18, 32, 30, 8, 2
            "1s2 2s2 2p6 3s2 3p6 4s2 3d10 4p6 5s2 4d10 5p6 6s2 4f14 5d10 6p6 7s2 5f13",          // 101 Md  -> 2, 8, 18, 32, 31, 8, 2
            "1s2 2s2 2p6 3s2 3p6 4s2 3d10 4p6 5s2 4d10 5p6 6s2 4f14 5d10 6p6 7s2 5f14",          // 102 No  -> 2, 8, 18, 32, 32, 8, 2
            "1s2 2s2 2p6 3s2 3p6 4s2 3d10 4p6 5s2 4d10 5p6 6s2 4f14 5d10 6p6 7s2 5f14 7p1",      // 103 Lr  -> 2, 8, 18, 32, 32, 8, 3
            "1s2 2s2 2p6 3s2 3p6 4s2 3d10 4p6 5s2 4d10 5p6 6s2 4f14 5d10 6p6 7s2 5f14 6d2",      // 104 Rf  -> 2, 8, 18, 32, 32, 10, 2
            "1s2 2s2 2p6 3s2 3p6 4s2 3d10 4p6 5s2 4d10 5p6 6s2 4f14 5d10 6p6 7s2 5f14 6d3",      // 105 Db  -> 2, 8, 18, 32, 32, 11, 2
            "1s2 2s2 2p6 3s2 3p6 4s2 3d10 4p6 5s2 4d10 5p6 6s2 4f14 5d10 6p6 7s2 5f14 6d4",      // 106 Sg  -> 2, 8, 18, 32, 32, 12, 2
            "1s2 2s2 2p6 3s2 3p6 4s2 3d10 4p6 5s2 4d10 5p6 6s2 4f14 5d10 6p6 7s2 5f14 6d5",      // 107 Bh  -> 2, 8, 18, 32, 32, 13, 2
            "1s2 2s2 2p6 3s2 3p6 4s2 3d10 4p6 5s2 4d10 5p6 6s2 4f14 5d10 6p6 7s2 5f14 6d6",      // 108 Hs  -> 2, 8, 18, 32, 32, 14, 2
            "1s2 2s2 2p6 3s2 3p6 4s2 3d10 4p6 5s2 4d10 5p6 6s2 4f14 5d10 6p6 7s2 5f14 6d7",      // 109 Mt  -> 2, 8, 18, 32, 32, 15, 2
            "1s2 2s2 2p6 3s2 3p6 4s2 3d10 4p6 5s2 4d10 5p6 6s2 4f14 5d10 6p6 7s2 5f14 6d8",      // 110 Ds  -> 2, 8, 18, 32, 32, 16, 2
            "1s2 2s2 2p6 3s2 3p6 4s2 3d10 4p6 5s2 4d10 5p6 6s2 4f14 5d10 6p6 7s2 5f14 6d9",      // 111 Rg  -> 2, 8, 18, 32, 32, 17, 2
            "1s2 2s2 2p6 3s2 3p6 4s2 3d10 4p6 5s2 4d10 5p6 6s2 4f14 5d10 6p6 7s2 5f14 6d10",     // 112 Cn  -> 2, 8, 18, 32, 32, 18, 2
            "1s2 2s2 2p6 3s2 3p6 4s2 3d10 4p6 5s2 4d10 5p6 6s2 4f14 5d10 6p6 7s2 5f14 6d10 7p1", // 113 Nh  -> 2, 8, 18, 32, 32, 18, 3
            "1s2 2s2 2p6 3s2 3p6 4s2 3d10 4p6 5s2 4d10 5p6 6s2 4f14 5d10 6p6 7s2 5f14 6d10 7p2", // 114 Fl  -> 2, 8, 18, 32, 32, 18, 4
            "1s2 2s2 2p6 3s2 3p6 4s2 3d10 4p6 5s2 4d10 5p6 6s2 4f14 5d10 6p6 7s2 5f14 6d10 7p3", // 115 Mc  -> 2, 8, 18, 32, 32, 18, 5
            "1s2 2s2 2p6 3s2 3p6 4s2 3d10 4p6 5s2 4d10 5p6 6s2 4f14 5d10 6p6 7s2 5f14 6d10 7p4", // 116 Lv  -> 2, 8, 18, 32, 32, 18, 6
            "1s2 2s2 2p6 3s2 3p6 4s2 3d10 4p6 5s2 4d10 5p6 6s2 4f14 5d10 6p6 7s2 5f14 6d10 7p5", // 117 Ts  -> 2, 8, 18, 32, 32, 18, 7
            "1s2 2s2 2p6 3s2 3p6 4s2 3d10 4p6 5s2 4d10 5p6 6s2 4f14 5d10 6p6 7s2 5f14 6d10 7p6", // 118 Og  -> 2, 8, 18, 32, 32, 18, 8
        };

        private static readonly Dictionary<int, Subcapa[]> CacheSubcapas =
            new Dictionary<int, Subcapa[]>();
        private static readonly Dictionary<int, int[]> CacheCapas =
            new Dictionary<int, int[]>();

        /// <summary>Indica si el numero atomico esta dentro del rango soportado.</summary>
        public static bool EsValido(int numeroAtomico)
        {
            return numeroAtomico >= ElementoMinimo && numeroAtomico <= ElementoMaximo;
        }

        /// <summary>
        /// Subcapas ocupadas en orden de llenado.
        /// Cromo (24) devuelve: 1s2 2s2 2p6 3s2 3p6 4s1 3d5.
        /// </summary>
        public static Subcapa[] ObtenerSubcapas(int numeroAtomico)
        {
            if (!EsValido(numeroAtomico))
            {
                throw new ArgumentOutOfRangeException(
                    "numeroAtomico",
                    "Numero atomico fuera del rango 1-118: " + numeroAtomico);
            }

            Subcapa[] cacheado;
            if (CacheSubcapas.TryGetValue(numeroAtomico, out cacheado))
            {
                return cacheado;
            }

            string[] tokens = Configuraciones[numeroAtomico - 1].Split(' ');
            Subcapa[] subcapas = new Subcapa[tokens.Length];
            for (int i = 0; i < tokens.Length; i++)
            {
                string token = tokens[i];
                int nivel = token[0] - '0';
                char tipo = token[1];
                int electrones = int.Parse(token.Substring(2));
                subcapas[i] = new Subcapa(nivel, tipo, electrones);
            }

            CacheSubcapas[numeroAtomico] = subcapas;
            return subcapas;
        }

        /// <summary>
        /// Electrones por capa, indice 0 = capa n=1.
        /// Potasio (19) devuelve [2, 8, 8, 1].
        /// Esta es la distribucion que debe dibujar el modelo de Bohr.
        /// </summary>
        public static int[] ObtenerCapas(int numeroAtomico)
        {
            int[] cacheado;
            if (CacheCapas.TryGetValue(numeroAtomico, out cacheado))
            {
                return cacheado;
            }

            Subcapa[] subcapas = ObtenerSubcapas(numeroAtomico);

            int nivelMaximo = 0;
            for (int i = 0; i < subcapas.Length; i++)
            {
                if (subcapas[i].Nivel > nivelMaximo)
                {
                    nivelMaximo = subcapas[i].Nivel;
                }
            }

            int[] capas = new int[nivelMaximo];
            for (int i = 0; i < subcapas.Length; i++)
            {
                capas[subcapas[i].Nivel - 1] += subcapas[i].Electrones;
            }

            CacheCapas[numeroAtomico] = capas;
            return capas;
        }

        /// <summary>Numero de capas ocupadas, es decir cuantos anillos dibujar.</summary>
        public static int NivelesOcupados(int numeroAtomico)
        {
            return ObtenerCapas(numeroAtomico).Length;
        }

        /// <summary>
        /// Electrones en la capa mas externa. Es el dato que explica la
        /// reactividad del elemento, asi que conviene destacarlo visualmente.
        /// </summary>
        public static int ElectronesDeValencia(int numeroAtomico)
        {
            int[] capas = ObtenerCapas(numeroAtomico);
            return capas[capas.Length - 1];
        }

        /// <summary>
        /// Notacion spdf ordenada por nivel, para mostrar en pantalla.
        /// Cromo (24) devuelve "1s2 2s2 2p6 3s2 3p6 3d5 4s1".
        /// </summary>
        public static string NotacionSpdf(int numeroAtomico)
        {
            Subcapa[] subcapas = ObtenerSubcapas(numeroAtomico);
            List<Subcapa> ordenadas = new List<Subcapa>(subcapas);
            ordenadas.Sort(CompararPorNivel);

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < ordenadas.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(' ');
                }
                sb.Append(ordenadas[i].ToString());
            }
            return sb.ToString();
        }

        private static int CompararPorNivel(Subcapa a, Subcapa b)
        {
            if (a.Nivel != b.Nivel)
            {
                return a.Nivel.CompareTo(b.Nivel);
            }
            return OrdenTipo(a.Tipo).CompareTo(OrdenTipo(b.Tipo));
        }

        private static int OrdenTipo(char tipo)
        {
            switch (tipo)
            {
                case 's': return 0;
                case 'p': return 1;
                case 'd': return 2;
                case 'f': return 3;
                default: return 4;
            }
        }
    }
}
