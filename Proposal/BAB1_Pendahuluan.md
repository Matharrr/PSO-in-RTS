# BAB I PENDAHULUAN

Bab ini memaparkan dasar dan arah penelitian tugas akhir yang berjudul *"Optimasi Bobot Artificial Neural Network Menggunakan Particle Swarm Optimization dengan Penambahan Parameter Dangerousness dan Attack Range pada NPC Game Real-Time Strategy"*. Secara berurutan, bab ini menguraikan latar belakang penelitian, rumusan masalah, research question beserta hipotesis, tujuan penelitian, pemangku kepentingan dan manfaat hasil penelitian, dukungan data, serta ruang lingkup dan batasan penelitian.

---

## 1.1 Latar Belakang

Industri game video terus berkembang pesat dan menjadi salah satu sektor hiburan digital terbesar di dunia. Salah satu genre yang memiliki kompleksitas tinggi dari sisi kecerdasan buatan (Artificial Intelligence/AI) adalah Real-Time Strategy (RTS). Dalam game RTS, pemain dituntut untuk mengelola sumber daya, membangun infrastruktur, melatih unit militer, dan mengalahkan lawan secara simultan dalam waktu nyata [Widhiyasana et al., 2022]. Kualitas pengalaman bermain pada game RTS sangat dipengaruhi oleh perilaku Non-Playable Character (NPC) yang berperan sebagai unit pasukan. NPC yang mampu mengambil keputusan secara adaptif dan menyerupai perilaku manusia akan menghasilkan pengalaman bermain yang lebih menantang dan imersif.

Pengembangan NPC yang *human-like* merupakan tantangan utama dalam AI game. Pendekatan yang umum digunakan adalah Artificial Neural Network (ANN), yaitu metode komputasi yang meniru cara kerja jaringan saraf biologis. ANN mampu memetakan kondisi lingkungan permainan menjadi keputusan tindakan unit secara real-time [Abiodun et al., 2018]. Namun, performa ANN sangat bergantung pada kualitas nilai bobot yang digunakan. Penentuan bobot yang optimal secara manual tidak memungkinkan karena jumlah bobot yang besar, sehingga diperlukan algoritma optimasi.

Widhiyasana et al. (2022) telah melakukan penelitian menggunakan Genetic Algorithm (GA) sebagai metode optimasi bobot pada ANN untuk mengendalikan NPC dalam simulasi game RTS berbasis Unity C#. Penelitian tersebut berhasil menunjukkan bahwa GA dapat digunakan untuk memperoleh bobot ANN yang optimal dengan crossover rate terbaik sebesar 0,6 dan mutation rate terbaik sebesar 0,09. Arsitektur ANN yang digunakan terdiri dari 37 neuron input, 18 neuron hidden, dan 3 neuron output, menghasilkan 720 bobot yang harus dioptimalkan. Sistem tersebut mengontrol 56 unit NPC yang terbagi menjadi dua tim, masing-masing terdiri dari 7 tipe unit.

Meskipun GA terbukti efektif, algoritma ini memiliki sejumlah keterbatasan. GA memerlukan operasi crossover dan mutation yang kompleks, serta rentan terhadap konvergensi prematur akibat hilangnya keragaman populasi [Idrissi et al., 2016]. Sebagai alternatif, Particle Swarm Optimization (PSO) hadir sebagai algoritma optimasi berbasis populasi yang terinspirasi dari perilaku kawanan burung atau ikan. PSO bekerja dengan cara setiap partikel (solusi kandidat) bergerak di ruang solusi berdasarkan pengalaman terbaiknya sendiri (*cognitive*) dan pengalaman terbaik seluruh kawanan (*social*). PSO umumnya memiliki parameter yang lebih sedikit, konvergensi lebih cepat, dan lebih mudah diimplementasikan dibandingkan GA [Kennedy & Eberhart, 1995].

Di sisi lain, penelitian Widhiyasana et al. (2022) belum mempertimbangkan dua aspek taktis penting dalam peperangan, yaitu *dangerousness* (tingkat bahaya musuh berdasarkan damage yang dapat diberikan per region) dan *attack range awareness* (kesadaran terhadap ancaman serangan jarak jauh per region). Ketiadaan kedua informasi ini menyebabkan NPC tidak dapat membuat keputusan taktis yang lebih matang, seperti menghindari zona berbahaya atau memprioritaskan target musuh berjarak jauh yang mengancam. Penambahan kedua parameter ini berpotensi meningkatkan kualitas pengambilan keputusan NPC secara signifikan.

Berdasarkan kesenjangan tersebut, penelitian ini mengusulkan pengembangan sistem NPC berbasis ANN dengan dua kontribusi utama: (1) mengganti algoritma optimasi dari GA menjadi PSO, dan (2) menambahkan 8 neuron input baru yaitu 4 input *dangerousness* dan 4 input *attack range awareness* per region, sehingga arsitektur ANN menjadi 45 input → 18 hidden → 3 output dengan total 864 bobot. Untuk mengukur kontribusi masing-masing pengembangan secara adil, penelitian ini dirancang dalam empat kombinasi eksperimen yang dibandingkan terhadap baseline paper asli.

---

## 1.2 Rumusan Masalah

Berdasarkan uraian pada latar belakang, permasalahan dalam penelitian ini dapat dirumuskan sebagai berikut:

1. Algoritma Genetic Algorithm (GA) yang digunakan pada penelitian Widhiyasana et al. (2022) memiliki keterbatasan berupa kompleksitas operasi genetik dan potensi konvergensi prematur, sehingga dibutuhkan algoritma optimasi alternatif yang lebih efisien dalam menjelajahi ruang solusi bobot ANN yang besar (720 bobot) untuk mengoptimalkan perilaku NPC dalam game RTS.

2. Arsitektur ANN pada paper acuan hanya menggunakan 37 input yang tidak mencakup informasi *dangerousness* (ancaman damage musuh) dan *attack range awareness* (ancaman serangan jarak jauh musuh) per region, sehingga NPC tidak memiliki dasar informasi yang cukup untuk mengambil keputusan taktis defensif maupun ofensif yang lebih adaptif terhadap kondisi medan pertempuran.

3. Belum diketahui apakah penggantian GA dengan PSO dan penambahan parameter *dangerousness* serta *attack range* secara bersamaan dapat meningkatkan win rate pasukan NPC dibandingkan dengan sistem baseline yang menggunakan GA dan ANN lama.

---

## 1.3 Research Question dan Hipotesis

### Research Question

Berdasarkan rumusan masalah yang telah diuraikan, penelitian ini dilandasi oleh tiga research question berikut:

- **RQ1**: Apakah Particle Swarm Optimization (PSO) menghasilkan win rate NPC yang lebih tinggi dibandingkan Genetic Algorithm (GA) ketika keduanya digunakan untuk mengoptimalkan bobot ANN dengan arsitektur yang sama (37 input → 18 hidden → 3 output)?

- **RQ2**: Apakah penambahan 8 neuron input baru (*dangerousness* dan *attack range awareness*) pada arsitektur ANN menghasilkan win rate NPC yang lebih tinggi dibandingkan ANN lama dengan arsitektur 37 input, ketika keduanya dioptimalkan menggunakan algoritma yang sama?

- **RQ3**: Apakah kombinasi PSO dengan ANN baru (45 input → 18 hidden → 3 output) menghasilkan win rate NPC tertinggi dibandingkan tiga kombinasi lainnya (GA+ANN lama, PSO+ANN lama, GA+ANN baru) dalam simulasi game RTS?

### Hipotesis

Berdasarkan teori dan kajian penelitian sebelumnya, hipotesis penelitian ini dirumuskan sebagai berikut:

- **H1**: PSO menghasilkan win rate NPC yang lebih tinggi dibandingkan GA pada arsitektur ANN yang sama, karena PSO memiliki mekanisme *social learning* yang memungkinkan seluruh partikel memanfaatkan solusi terbaik global secara langsung tanpa operasi crossover/mutation, sehingga konvergensi lebih cepat dan stabil.

- **H2**: Penambahan parameter *dangerousness* dan *attack range awareness* meningkatkan win rate NPC karena memperkaya informasi kontekstual yang tersedia bagi ANN dalam menentukan tindakan unit, khususnya untuk keputusan taktis defensif dan prioritas target musuh.

- **H3**: Kombinasi PSO dengan ANN baru (45 input) menghasilkan win rate tertinggi karena menggabungkan keunggulan algoritma optimasi yang lebih efisien (PSO) dengan representasi lingkungan yang lebih lengkap (45 input).

---

## 1.4 Tujuan Penelitian

Berdasarkan research question dan hipotesis yang telah dirumuskan, tujuan penelitian ini adalah:

1. **Membuktikan** bahwa Particle Swarm Optimization (PSO) mampu menghasilkan win rate NPC yang lebih tinggi dibandingkan Genetic Algorithm (GA) dalam proses optimasi bobot ANN pada simulasi game RTS, untuk menentukan algoritma optimasi yang lebih unggul dalam konteks pengendalian unit NPC berbasis ANN.

2. **Membuktikan** bahwa penambahan parameter *dangerousness* dan *attack range awareness* sebagai neuron input ANN mampu meningkatkan win rate pasukan NPC, untuk menentukan arsitektur input ANN yang lebih representatif terhadap kondisi medan pertempuran dalam game RTS.

3. **Menentukan** kombinasi algoritma optimasi dan arsitektur ANN yang menghasilkan win rate NPC tertinggi di antara empat kombinasi eksperimen (GA+ANN lama, PSO+ANN lama, GA+ANN baru, PSO+ANN baru), untuk meningkatkan kualitas perilaku taktis NPC dalam simulasi game Real-Time Strategy.

---

## 1.5 Pemangku Kepentingan dan Manfaat Hasil Penelitian

### Pemangku Kepentingan

| No | Pemangku Kepentingan | Keterkaitan dengan Penelitian |
|----|----------------------|-------------------------------|
| 1 | **Peneliti dan Akademisi di Bidang AI Game** | Memiliki kepentingan langsung terhadap hasil penelitian sebagai referensi pengembangan metode optimasi ANN untuk NPC |
| 2 | **Pengembang Game (Game Developer)** | Dapat memanfaatkan model dan temuan penelitian untuk mengimplementasikan NPC yang lebih cerdas dan adaptif dalam produk game RTS |
| 3 | **Institusi Pendidikan** | Penelitian ini berkontribusi pada pengembangan ilmu di bidang kecerdasan buatan dan game AI |
| 4 | **Pemain Game RTS** | Sebagai pengguna akhir yang mendapatkan manfaat berupa pengalaman bermain yang lebih menantang dan realistis |

### Manfaat Hasil Penelitian

1. **Manfaat Akademis**: Penelitian ini memberikan bukti empiris mengenai perbandingan performa PSO dan GA sebagai metode optimasi bobot ANN dalam konteks game AI, serta menunjukkan dampak penambahan fitur input *dangerousness* dan *attack range* terhadap kualitas pengambilan keputusan NPC. Hasil penelitian dapat dijadikan referensi ilmiah untuk penelitian lebih lanjut di bidang optimasi berbasis populasi pada game AI.

2. **Manfaat Praktis bagi Pengembang Game**: Pengembang game RTS dapat menggunakan model PSO+ANN yang dihasilkan sebagai acuan implementasi sistem NPC yang lebih adaptif dan taktis, tanpa harus melakukan eksplorasi parameter dari awal. Temuan mengenai parameter PSO optimal (inertia weight, cognitive coefficient, social coefficient) langsung dapat diterapkan dalam pipeline pengembangan game.

3. **Manfaat bagi Kualitas Pengalaman Bermain**: NPC yang mampu mempertimbangkan tingkat bahaya (*dangerousness*) dan jangkauan serangan musuh (*attack range*) akan berperilaku lebih taktis dan tidak terprediksi, sehingga meningkatkan tantangan dan kepuasan pemain dalam memainkan game RTS.

---

## 1.6 Dukungan Data

Penelitian ini tidak menggunakan dataset eksternal dari sumber publik maupun organisasi tertentu. Data yang digunakan dalam eksperimen sepenuhnya dihasilkan secara *synthetic* melalui simulator game RTS yang dikembangkan sendiri menggunakan Unity Engine dengan bahasa pemrograman C#, mengacu pada desain simulator yang digunakan oleh Widhiyasana et al. (2022).

Data eksperimen yang dihasilkan meliputi:

- **Data Fitness per Generasi**: Nilai fitness setiap kromosom/partikel pada setiap generasi/iterasi, disimpan dalam format `.csv`.
- **Data Bobot ANN**: Nilai bobot terbaik (864 bobot untuk ANN baru, 720 bobot untuk ANN lama) setelah proses optimasi selesai, disimpan dalam format `.csv`.
- **Data Win Rate**: Persentase kemenangan pasukan pada setiap skenario pengukuran, disimpan dalam format `.csv`.

Formasi awal pasukan ditentukan secara acak pada setiap skenario untuk memastikan generalisasi hasil. Total eksperimen mencakup 4 kombinasi perlakuan dengan 20 skenario pengukuran dan maksimal 4000 iterasi, menghasilkan data yang cukup untuk analisis komparatif antar kombinasi eksperimen. Detail struktur data, format penyimpanan, dan penjelasan setiap variabel disampaikan pada Sub Bab III.4.

---

## 1.7 Ruang Lingkup dan Batasan

### Ruang Lingkup

Penelitian ini mencakup hal-hal berikut:

1. Pengembangan dan pengujian algoritma PSO sebagai metode optimasi bobot ANN untuk pengendalian NPC dalam simulasi game RTS.
2. Perancangan arsitektur ANN baru dengan 45 neuron input (37 input lama + 4 input *dangerousness* + 4 input *attack range awareness*), 18 neuron hidden, dan 3 neuron output.
3. Implementasi simulator game RTS berbasis Unity Engine (C#) yang mengakomodasi 4 kombinasi eksperimen: GA+ANN lama, PSO+ANN lama, GA+ANN baru, PSO+ANN baru.
4. Evaluasi dan perbandingan performa antar kombinasi eksperimen menggunakan metrik win rate pasukan dalam kondisi simulasi.
5. Penggunaan 7 tipe unit (Swordman, Archer, Spearman, Axeman, Heavy, Very Heavy, Cavalry) dengan total 56 unit (28 per tim) sesuai desain paper acuan.

### Batasan

1. Penelitian ini hanya mengimplementasikan simulasi game RTS, bukan game RTS yang dipublikasikan secara komersial. Simulator dikembangkan khusus untuk keperluan eksperimen ilmiah.
2. Evaluasi performa hanya menggunakan metrik **win rate** sebagai indikator keberhasilan, tidak mencakup metrik subjektif seperti tingkat *believability* atau kepuasan pemain.
3. Parameter PSO yang diuji dibatasi pada kombinasi nilai inertia weight (w), cognitive coefficient (c1), dan social coefficient (c2) yang ditentukan berdasarkan studi literatur; tidak dilakukan grid search parameter secara menyeluruh.
4. Struktur hidden layer (18 neuron) dan output layer (3 neuron) serta fungsi aktivasi sigmoid **tidak diubah** dari paper acuan untuk menjaga keadilan perbandingan antar eksperimen.
5. Implementasi GA pada eksperimen 1 dan 3 menggunakan parameter terbaik dari paper acuan (crossover rate 0,6 dan mutation rate 0,09) sebagai baseline yang sudah tervalidasi.
6. Penelitian tidak mencakup penerapan sistem pada game RTS multi-pemain secara nyata (online/multiplayer); seluruh eksperimen dilakukan dalam lingkungan simulasi terkontrol.
7. Durasi setiap battle dalam simulasi dibatasi **10 detik** dengan formasi awal pasukan yang ditentukan secara acak, sesuai dengan desain eksperimen paper acuan.
8. Jumlah maksimal iterasi/generasi optimasi adalah **4000**, sesuai batasan yang ditetapkan pada penelitian acuan.

---

## Referensi Bab I

- Widhiyasana, Y., Harika, M., Hakim, F. F. N., Diani, F., Komariah, K. S., & Ramdania, D. R. (2022). Genetic Algorithm for Artificial Neural Networks in Real-Time Strategy Games. *JOIV: International Journal on Informatics Visualization*, 6(2), 298–305.
- Abiodun, O. I., Jantan, A., Omolara, A. E., Dada, K. V., Mohamed, N. A. E., & Arshad, H. (2018). State-of-the-art in artificial neural network applications: A survey. *Heliyon*, 4(11).
- Kennedy, J., & Eberhart, R. (1995). Particle swarm optimization. *Proceedings of ICNN'95 - International Conference on Neural Networks*, 4, 1942–1948.
- Idrissi, M. A. J., Ramchoun, H., Ghanou, Y., & Ettaouil, M. (2016). Genetic algorithm for neural network architecture optimization. *Proceedings of 3rd IEEE International Conference on Logistics Operations Management (GOL 2016)*.
