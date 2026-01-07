# VR Warehouse Management Training Simulator

[![Unity](https://img.shields.io/badge/Unity-2022.3-black?logo=unity)](https://unity.com/)
[![C#](https://img.shields.io/badge/C%23-10.0-blue?logo=c-sharp)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![Firebase](https://img.shields.io/badge/Firebase-Realtime%20DB-orange?logo=firebase)](https://firebase.google.com/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

> Educational VR training system that simulates warehouse management workflows, validated with 30 users achieving "Good" usability (SUS: 71.25).

![Warehouse Simulator Screenshot](screenshots/main-view.png)

---

## 🎯 About

An immersive VR training simulator designed to bridge the gap between theoretical education and practical skills in agroindustrial logistics. The system replicates real-world warehouse operations without expensive physical infrastructure.

**🏆 Research Project** - Universitas Gadjah Mada (Aug 2024 - Sep 2025)

### Key Features

- ✅ **Virtual RFID System** - Simulates real-world RFID workflow (inbound, racking, outbound)
- 📊 **Real-time WMS Sync** - Firebase Realtime Database integration
- 🎮 **Interactive 3D Environment** - Physics-based interactions, realistic operations
- 📈 **Validated Performance** - SUS score 71.25 ("Good"), outperformed real systems in UX
- 👥 **30 User Testing** - Validated with agricultural students

---

## 📸 Screenshots

| Main Environment | RFID Workflow | Dashboard |
|-----------------|---------------|-----------|
| ![](screenshots/env.png) | ![](screenshots/rfid.png) | ![](screenshots/dash.png) |

---

## 🛠️ Tech Stack

- **Platform:** Unity 2022.3 LTS
- **Language:** C#
- **VR SDK:** XR Interaction Toolkit
- **Database:** Firebase Realtime Database
- **3D Modeling:** Blender
- **Version Control:** Git

---

## 🚀 Installation & Setup

### Prerequisites

- Unity 2022.3.50f1
- VR headset (Meta Quest 2/3, HTC Vive, or compatible)
- Firebase account (for WMS synchronization)

### Setup Steps

1. **Clone Repository**
```bash
   git clone https://github.com/kasjaduddin/warehouse-simulation.git
   cd warehouse-simulation
```

2. **Open in Unity**
   - Open Unity Hub
   - Click "Add project from disk"
   - Select cloned folder
   - Open project

3. **Configure Firebase**
   - Create Firebase project at console.firebase.google.com
   - Download `google-services.json`
   - Place in `Assets/` folder
   - Update Firebase settings in Unity

4. **Build & Run**
   - File → Build Settings
   - Select target platform (Android for Quest, PC for tethered VR)
   - Build and Run

---

## 📊 Project Structure
```
warehouse-simulation/
├── Assets/
│   ├── Scenes/              # Unity scenes
│   ├── Scripts/             # C# scripts
│   │   ├── Notepad/         # Notepad logic
│   │   ├── RFID/            # RFID simulation logic
│   │   └── WMS/             # Warehose Management System simulation logic
│   ├── Models/              # 3D models
│   ├── Materials/           # Textures & materials
│   └── Prefabs/             # Reusable objects
├── Screenshots/             # Documentation images
└── README.md
```

---

## 🎮 How to Use

1. **Put on VR Headset** - Launch application
2. **Practice Mode** - Simulate warehouse operations:
   - Scan RFID tags (virtual)
   - Register incoming items
   - Place items in racks
   - Process outbound orders

---

## 📈 Validation Results

### Usability Testing (n=30)
- **SUS Score:** 71.25 ("Good" rating)
- **Comparison:** Significantly outperformed real WMS in UX (UEQ)
- **Participants:** Agricultural engineering students
- **Testing Period:** September - November 2024

### Key Findings
✅ Effective for educational training  
✅ Cost-effective alternative to physical infrastructure  
✅ Engaging and intuitive interaction  
✅ Realistic workflow simulation  

---

## 🔧 Development

### Running in Development
```bash
# Unity Editor
1. Open Unity Hub
2. Load project
3. Press Play in Editor

# VR Testing
1. Connect VR headset
2. Enable VR mode in Unity
3. Play in Editor or Build & Run
```

### Code Style

- Follow Microsoft C# Coding Conventions
- Use meaningful variable/method names
- Comment complex logic
- Use regions for code organization

---

## 🚧 Roadmap

- [ ] Add more warehouse scenarios (cold storage, hazmat)
- [ ] Implement multiplayer mode for collaborative training
- [ ] Add voice commands for hands-free operation
- [ ] Develop assessment analytics dashboard
- [ ] Support for additional VR platforms

---

## 📝 Research Paper

This project is part of ongoing research at UGM. Academic paper currently under review.

**Focus:** VR training effectiveness for agroindustrial warehouse management education.

---

## 📄 License

MIT License - See [LICENSE](LICENSE) file for details

---

## 👨‍💻 Author

**Kholil Asjaduddin**
- LinkedIn: [linkedin.com/in/kholil-asjaduddin](https://www.linkedin.com/in/kholil-asjaduddin/)
- Email: kasjaduddin@outlook.com
- GitHub: [@kasjaduddin](https://github.com/kasjaduddin)

---

## 🙏 Acknowledgments

- Universitas Gadjah Mada - Department of Electrical and Information Engineering
- Research supervisors and collaborators
- 30 participants who provided valuable feedback

---

<div align="center">

**⭐ If this project helped you, please consider giving it a star!**

Built with ❤️ using Unity and C#

</div>
