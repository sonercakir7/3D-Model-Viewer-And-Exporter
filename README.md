# 3D Model Viewer and Exporter

![License](https://img.shields.io/badge/license-MIT-blue.svg)
![Platform](https://img.shields.io/badge/platform-Windows-blue)
![Language](https://img.shields.io/badge/language-C%23%20%7C%20WPF-purple)

**[English]**  
A comprehensive and lightweight 3D model viewer and exporter application built with WPF, Helix Toolkit, and AssimpNet. This tool allows users to view, analyze, and convert 3D models between various popular formats with ease. This project is a hobby project, written and developed entirely from scratch.

**[Türkçe]**  
WPF, Helix Toolkit ve AssimpNet kullanılarak geliştirilmiş kapsamlı ve hafif bir 3D model görüntüleyici ve dışa aktarıcı uygulaması. Bu araç, kullanıcıların 3D modelleri görüntülemesini, analiz etmesini ve çeşitli popüler formatlar arasında kolayca dönüştürmesini sağlar. Bu proje hobi amaçlı geliştirilmiş olup tamamen sıfırdan yazılıp hazırlanmıştır.

---

## 🌟 Features / Özellikler

### 🇬🇧 English
*   **Wide Format Support:** Load and view `OBJ`, `STL`, `FBX`, `GLTF`, `GLB`, `3DS`, `DAE`, `PLY` and more.
*   **Model Conversion:** Export loaded models to `FBX`, `GLB`, `GLTF`, `OBJ`, or `STL`.
*   **Sub-mesh Management:** View individual mesh parts and export specific sub-meshes separately.
*   **Texture Handling:** View embedded or external textures and export them to `PNG` or `JPG`.
*   **Advanced Visualization:**
    *   Orbit, Zoom, and Pan controls.
    *   Switch between **Perspective** and **Orthographic** cameras.
    *   Standard views (Front, Top, Left, etc.).
    *   Wireframe and Solid rendering modes.
    *   Dark/Light theme support.
*   **Model Analysis:**
    *   Vertex and Polygon counts.
    *   Real-time dimensions (X, Y, Z) and Volume calculation.
    *   Unit conversion (mm, cm, m).
*   **User Friendly:**
    *   Drag & Drop support.
    *   Recent files history.
    *   Multilingual interface (English, Turkish, German, French).
    *   Screenshot capability.

### 🇹🇷 Türkçe
*   **Geniş Format Desteği:** `OBJ`, `STL`, `FBX`, `GLTF`, `GLB`, `3DS`, `DAE`, `PLY` ve daha fazlasını açın ve görüntüleyin.
*   **Model Dönüştürme:** Yüklenen modelleri `FBX`, `GLB`, `GLTF`, `OBJ` veya `STL` formatlarına dışa aktarın.
*   **Alt Mesh Yönetimi:** Model parçalarını (mesh) ayrı ayrı görüntüleyin ve seçili parçaları tek başına dışa aktarın.
*   **Doku (Texture) İşlemleri:** Gömülü veya harici dokuları görüntüleyin ve `PNG` veya `JPG` olarak kaydedin.
*   **Gelişmiş Görüntüleme:**
    *   Yörünge (Orbit), Yakınlaştırma (Zoom) ve Kaydırma (Pan) kontrolleri.
    *   **Perspektif** ve **Ortografik** kamera modları arasında geçiş.
    *   Standart görünümler (Ön, Üst, Sol vb.).
    *   Tel kafes (Wireframe) ve Katı (Solid) görüntüleme modları.
    *   Koyu/Açık tema desteği.
*   **Model Analizi:**
    *   Vertex (Köşe) ve Poligon sayıları.
    *   Gerçek zamanlı boyutlar (X, Y, Z) ve Hacim hesaplama.
    *   Birim dönüştürme (mm, cm, m).
*   **Kullanıcı Dostu:**
    *   Sürükle ve Bırak desteği.
    *   Son açılan dosyalar geçmişi.
    *   Çoklu dil desteği (İngilizce, Türkçe, Almanca, Fransızca).
    *   Ekran görüntüsü alma özelliği.

---

## 🛠 Supported Formats / Desteklenen Formatlar

| Feature | Extensions |
| :--- | :--- |
| **Import (Yükleme)** | `.obj`, `.stl`, `.fbx`, `.gltf`, `.glb`, `.3ds`, `.dae`, `.ply` |
| **Export (Dışa Aktarma)** | `.fbx`, `.glb`, `.gltf`, `.obj`, `.stl` |

---

## 🚀 Getting Started / Başlarken

### Requirements / Gereksinimler
*   Windows OS
*   .NET Desktop Runtime (compatible with project version)

### Installation / Kurulum
1.  Clone the repository:
    ```bash
    git clone https://github.com/sonercakir7/3D-Model-Viewer-And-Exporter.git
    ```
2.  Open the solution in Visual Studio.
3.  Restore NuGet packages.
4.  Build and Run.

### Usage / Kullanım
*   **Open:** File -> Open or Drag & Drop a model file.
*   **Rotate:** Right-click + Drag.
*   **Pan:** Middle-click + Drag (or Shift + Right-click).
*   **Zoom:** Mouse Wheel.
*   **Export:** Use the "Export" menu to save the full scene or right-click a mesh in the list to save individually.

---

## 📄 License
This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.