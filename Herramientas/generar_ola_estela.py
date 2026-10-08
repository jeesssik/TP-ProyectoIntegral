"""Genera la animacion de la ola que levanta la helice de la lancha al arrancar.

La lancha acelera hacia +x, la helice tira el agua hacia -x: nace un hervidero
y un chorro de espuma en la popa, y una ola rompiente corre hacia la izquierda
pasando por encima del muelle. Dos capas: detras (cuerpo de la ola, atras de
Grace) y delante (lamina que pasa por encima de las tablas y chorrea).

Las curvas front_x / front_h tienen que coincidir con OlaSalpicon.cs.
"""
import os
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

RAIZ = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SALIDA = os.path.join(RAIZ, "Assets", "Resources", "Ola", "estela")

W, H = 1600, 720
S = 2
w, h = W * S, H * S
BASE = 110
PIVOTE_X = 0.9
N = 30
ALTURA_MAX = 380.0

ESPUMA = np.array([238, 249, 252], float)
ESPUMA_SOMBRA = np.array([178, 222, 236], float)
CLARO = np.array([96, 196, 216], float)
MEDIO = np.array([40, 138, 172], float)
PROFUNDO = np.array([18, 84, 120], float)
VETA = np.array([150, 222, 236], float)
CONTORNO = np.array([14, 50, 72], float)


def ss(a, b, x):
    t = np.clip((x - a) / (b - a), 0.0, 1.0)
    return t * t * (3.0 - 2.0 * t)


def front_x(p):
    q = np.clip((p - 0.08) / 0.72, 0.0, 1.0)
    return 0.86 - 0.8 * (1.0 - (1.0 - q) ** 1.7)


def front_h(p):
    return ALTURA_MAX * ss(0.04, 0.3, p) * (1 - 0.75 * ss(0.5, 0.85, p)) * (1 - ss(0.85, 1.0, p))


def nivel_inundacion(p):
    return 105.0 * ss(0.06, 0.3, p) * (1 - ss(0.6, 0.98, p))


def hervidero(p):
    return 120.0 * ss(0.0, 0.08, p) * (1 - ss(0.25, 0.6, p))


def inclinacion(p):
    return 0.065 * ss(0.15, 0.4, p) * (1 - 0.6 * ss(0.65, 1.0, p))


def ruido(semilla, celdas_x, celdas_y, ancho, alto):
    rng = np.random.default_rng(semilla)
    base = (rng.random((celdas_y, celdas_x)) * 255).astype(np.uint8)
    img = Image.fromarray(base, "L").resize((ancho, alto), Image.BICUBIC)
    return np.asarray(img, float) / 255.0


def superficie(u, p, nz):
    xf = front_x(p)
    hf = front_h(p)
    lado = 1.0 / (1.0 + np.exp(-(u - xf) / 0.008))
    centro = xf + 0.05
    ancho = np.where(u < centro, 0.045, 0.15)
    cresta = hf * np.exp(-((u - centro) / ancho) ** 2) * lado
    altura = np.maximum(cresta, nivel_inundacion(p) * lado)
    altura = altura + hervidero(p) * np.exp(-((u - PIVOTE_X) / 0.035) ** 2)
    actividad = ss(0.02, 0.2, p) * (1 - ss(0.85, 1.0, p))
    oleaje = 7 * np.sin(2 * np.pi * (9 * u + 2.2 * p)) + 4 * np.sin(2 * np.pi * (23 * u - 3.1 * p) + 1.3)
    altura = altura + (oleaje + 12 * (nz - 0.5)) * actividad * lado
    return altura * (1 - ss(0.925, 0.975, u))


def particulas(rng, cantidad, nacer, origen, vel_x, vel_y, radio):
    lista = []
    for _ in range(cantidad):
        t0 = rng.uniform(*nacer)
        lista.append({
            "t0": t0,
            "origen": origen,
            "vx": rng.uniform(*vel_x),
            "vy": rng.uniform(*vel_y),
            "r": rng.uniform(*radio),
            "dx": rng.uniform(-0.012, 0.012),
        })
    return lista


def posicion(part, p):
    dt = p - part["t0"]
    if dt < 0:
        return None
    ox, oy = part["origen"](part["t0"])
    x = (ox + part["dx"]) * W + part["vx"] * dt
    y = oy + part["vy"] * dt - 0.5 * 2600.0 * dt * dt
    if y < -30 or x < -20:
        return None
    r = part["r"] * (1.0 - ss(0.22, 0.5, dt))
    if r < 0.9:
        return None
    return x, y, r


def a_pixel(x, y):
    return x * S, (H - BASE - y) * S


def dibujar_gotas(capa, lista, p, alfa=1.0):
    dib = ImageDraw.Draw(capa)
    for part in lista:
        pos = posicion(part, p)
        if pos is None:
            continue
        x, y, r = pos
        px, py = a_pixel(x, y)
        rs = r * S
        borde = rs + 1.1 * S
        dib.ellipse([px - borde, py - borde, px + borde, py + borde],
                    fill=tuple(int(c) for c in CONTORNO) + (int(235 * alfa),))
        dib.ellipse([px - rs, py - rs, px + rs, py + rs],
                    fill=(196, 238, 248, int(255 * alfa)))
        brillo = rs * 0.45
        dib.ellipse([px - rs * 0.35 - brillo, py - rs * 0.35 - brillo,
                     px - rs * 0.35 + brillo, py - rs * 0.35 + brillo],
                    fill=(255, 255, 255, int(255 * alfa)))


def dibujar_bruma(lista, p):
    capa = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    dib = ImageDraw.Draw(capa)
    for part in lista:
        pos = posicion(part, p)
        if pos is None:
            continue
        x, y, r = pos
        px, py = a_pixel(x, y)
        rs = (r * 2.6 + 8) * S
        dib.ellipse([px - rs, py - rs, px + rs, py + rs], fill=(240, 250, 253, 38))
    return capa.filter(ImageFilter.GaussianBlur(9 * S))


def mezclar(a, b, t):
    return a + (b - a) * t[..., None]


def contorno(mascara, grosor):
    img = Image.fromarray((mascara * 255).astype(np.uint8), "L")
    dil = np.asarray(img.filter(ImageFilter.GaussianBlur(grosor * 0.45)), float) / 255.0
    return np.clip(ss(0.02, 0.12, dil) - mascara, 0.0, 1.0)


def componer(*capas):
    """Capas premultiplicadas (rgb, a), de abajo hacia arriba."""
    rgb = np.zeros((h, w, 3))
    a = np.zeros((h, w))
    for c_rgb, c_a in capas:
        rgb = c_rgb * c_a[..., None] + rgb * (1 - c_a[..., None])
        a = c_a + a * (1 - c_a)
    return rgb, a


def pil_a_capa(img):
    arr = np.asarray(img, float) / 255.0
    return arr[..., :3] * 255.0, arr[..., 3]


def reducir(rgb, a):
    pm = rgb * a[..., None]
    pm = pm.reshape(H, S, W, S, 3).mean(axis=(1, 3))
    al = a.reshape(H, S, W, S).mean(axis=(1, 3))
    seguro = np.where(al > 1e-4, al, 1.0)
    color = np.clip(pm / seguro[..., None], 0, 255)
    al = np.where(al < 1.5 / 255.0, 0.0, al)
    color[al == 0] = 0
    out = np.dstack([color, al * 255.0]).round().astype(np.uint8)
    return Image.fromarray(out, "RGBA")


def generar():
    os.makedirs(os.path.join(SALIDA, "detras"), exist_ok=True)
    os.makedirs(os.path.join(SALIDA, "delante"), exist_ok=True)

    rng = np.random.default_rng(11)
    popa = lambda t: (PIVOTE_X, 8.0)
    cresta = lambda t: (float(front_x(t)) + 0.03, float(front_h(t)) * 0.92)
    frente = lambda t: (float(front_x(t)) + 0.005, float(front_h(t)) * 0.45)

    chorro = particulas(rng, 80, (0.0, 0.4), popa, (-700, -200), (650, 1250), (3, 8))
    rocio = particulas(rng, 70, (0.16, 0.66), cresta, (-620, -140), (150, 600), (2.5, 6.5))
    gotas_frente = particulas(rng, 40, (0.16, 0.7), frente, (-560, -200), (120, 420), (2.5, 5.5))

    ancho_ruido = int(w * 1.6)
    n_grande = ruido(3, 96, 27, ancho_ruido, h)
    n_fino = ruido(5, 320, 90, ancho_ruido, h)
    n_linea = ruido(7, 140, 1, ancho_ruido, 1)[0]
    n_chorreo = ruido(9, 420, 6, ancho_ruido, h)

    Y = ((np.arange(h)[::-1] / S) - BASE)[:, None] * np.ones((1, w))
    U = (np.arange(w) / w)[None, :] * np.ones((h, 1))

    for i in range(N):
        p = i / (N - 1)
        off = int(p * 0.45 * w)
        ng = n_grande[:, off:off + w]
        nf = n_fino[:, off:off + w]
        nl = np.broadcast_to(n_linea[off:off + w][None, :], (h, w))
        nc = n_chorreo[:, off:off + w]
        xf = float(front_x(p))

        Xs = U + inclinacion(p) * np.clip(Y / ALTURA_MAX, 0, None) ** 2.4
        alto = superficie(Xs, p, nl)
        mascara = ((Y < alto) & (alto > 1.5)).astype(float)
        d = alto - Y

        color = mezclar(np.broadcast_to(CLARO, (h, w, 3)).copy(), MEDIO, ss(45, 90, d + 24 * (ng - 0.5)))
        color = mezclar(color, PROFUNDO, ss(125, 190, d + 34 * (ng - 0.5)))
        veta = ((((d + 32 * ng) % 46) < 3.6) & (d > 28)).astype(float)
        color = mezclar(color, VETA, veta * 0.55)
        espuma = 1 - ss(12, 34, d + 14 * (nf - 0.5))
        cara = np.exp(-((Xs - xf - 0.01) / 0.022) ** 2) * (nf > 0.44) * (Y > -4)
        espuma = np.maximum(espuma, cara * 0.95)
        tono_espuma = mezclar(np.broadcast_to(ESPUMA_SOMBRA, (h, w, 3)).copy(), ESPUMA, ss(0.35, 0.7, nf))
        color = color + (tono_espuma - color) * espuma[..., None]

        apagado = 1 - ss(0.9, 1.0, p)
        bajo_muelle = ss(-60, -8, Y)
        alfa_cuerpo = mascara * 0.97 * bajo_muelle * apagado
        borde = contorno(mascara, 2 * 3 * S + 1) * bajo_muelle * apagado

        bruma = pil_a_capa(dibujar_bruma(chorro + rocio, p))
        gotas = Image.new("RGBA", (w, h), (0, 0, 0, 0))
        dibujar_gotas(gotas, chorro + rocio, p)
        rgb, a = componer(
            (np.broadcast_to(CONTORNO, (h, w, 3)), borde),
            (color, alfa_cuerpo),
            bruma,
            pil_a_capa(gotas),
        )
        reducir(rgb, a).save(os.path.join(SALIDA, "detras", f"estela_detras_{i:02d}.png"))

        tope = 70 + 40 * nl
        lamina = mascara * (Y >= 0) * (Y < tope)
        df = tope - Y
        espuma_f = 1 - ss(5, 18, df + 8 * (nf - 0.5))
        color_f = mezclar(np.broadcast_to(np.array([122, 208, 224], float), (h, w, 3)).copy(),
                          ESPUMA, np.maximum(espuma_f, 0.4 * (nf > 0.62)))

        col_agua = superficie(U[0], p, nl[0])
        largo = (22 + 40 * n_linea[off:off + w]) * np.clip(col_agua / 45.0, 0, 1)
        largo = np.broadcast_to(largo[None, :], (h, w))
        chorreo = ((Y < 0) & (-Y < largo) & (largo > 2)).astype(float)
        caida = 1 - ss(0.55, 1.0, -Y / np.maximum(largo, 1))
        color_c = mezclar(np.broadcast_to(np.array([130, 214, 232], float), (h, w, 3)).copy(),
                          ESPUMA, ss(0.45, 0.8, nc))

        alfa_lamina = lamina * (0.66 + 0.3 * espuma_f) * apagado
        alfa_chorreo = chorreo * caida * (0.25 + 0.4 * ss(0.4, 0.8, nc)) * apagado
        borde_f = contorno(lamina, 2 * 2 * S + 1) * (Y >= 0) * 0.7 * apagado

        gotas_f = Image.new("RGBA", (w, h), (0, 0, 0, 0))
        dibujar_gotas(gotas_f, gotas_frente, p, 0.95)
        rgb, a = componer(
            (color_c, alfa_chorreo),
            (np.broadcast_to(CONTORNO, (h, w, 3)), borde_f),
            (color_f, alfa_lamina),
            pil_a_capa(gotas_f),
        )
        reducir(rgb, a).save(os.path.join(SALIDA, "delante", f"estela_delante_{i:02d}.png"))
        print(f"frame {i + 1}/{N}", flush=True)


def vista_previa(destino):
    lancha = Image.open(os.path.join(RAIZ, "Assets", "Prefabs", "Escenario", "LanchaPower.png")).convert("RGBA")
    escala = 0.55
    lancha = lancha.resize((int(lancha.width * escala), int(lancha.height * escala)), Image.LANCZOS)
    piv_x = int(PIVOTE_X * W)
    grace_x = piv_x - 443
    tiras = []
    ancho = W + 900
    for i in range(N):
        fondo = Image.new("RGBA", (ancho, H), (176, 208, 226, 255))
        lx = piv_x + 381 - lancha.width // 2
        ly = (H - BASE) - 154 - lancha.height // 2
        if i > 2:
            lx += int(((i - 2) / (N - 1)) ** 2 * 900)
        fondo.alpha_composite(lancha, (lx, ly))
        fondo.alpha_composite(Image.open(os.path.join(SALIDA, "detras", f"estela_detras_{i:02d}.png")))
        dib = ImageDraw.Draw(fondo)
        dib.rectangle([0, H - BASE, ancho, H], fill=(112, 82, 58, 255))
        dib.rectangle([0, H - BASE, ancho, H - BASE + 10], fill=(150, 112, 80, 255))
        dib.rectangle([grace_x - 40, H - BASE - 440, grace_x + 40, H - BASE], fill=(200, 70, 90, 255))
        fondo.alpha_composite(Image.open(os.path.join(SALIDA, "delante", f"estela_delante_{i:02d}.png")))
        if i in (8, 14, 20):
            fondo.save(destino.replace(".png", f"_{i:02d}.png"))
        tiras.append(fondo.resize((ancho // 5, H // 5), Image.LANCZOS))
    cols = 5
    filas = (N + cols - 1) // cols
    tw, th = ancho // 5, H // 5
    hoja = Image.new("RGBA", (cols * tw, filas * th), (0, 0, 0, 255))
    for i, t in enumerate(tiras):
        hoja.paste(t, ((i % cols) * tw, (i // cols) * th))
    hoja.save(destino)


if __name__ == "__main__":
    if "--solo-vista" not in sys.argv:
        generar()
    vista_previa(os.path.join(RAIZ, "Herramientas", "vista_ola_estela.png"))
