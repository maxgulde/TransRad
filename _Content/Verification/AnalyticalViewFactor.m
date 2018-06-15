% Analytical view factor determination for two parallel rectangles
% Equation from "Thermal Radiation HEat Transfer" 5th Edition Appendix 5
% Updated: 2018-06-07

clear
clc

% Options
f_ExportFigures = 1;
p_BasePath = 'T:\software\TransRad\_Content\Verification\';

% % % Parallel Plates
a = 1;  % Rectangle length
b = 1;  % Rectangle width
dOffset = 0.1;
D = dOffset + (0.01:0.01:2);

DistVF = zeros(numel(D),2);

ii = 1;
for d = D
    x = a/d;
    y = b/d;

    kx = 1 + x^2;
    ky = 1 + y^2;
    A = 2/(pi*x*y);
    B = (kx*ky/(1 + x^2 + y^2))^0.5;
    C = x*sqrt(ky)*atan(x/sqrt(ky));
    D = y*sqrt(kx)*atan(y/sqrt(kx));
    E = x*atan(x);
    F = y*atan(y);

    F_A = A * (log(B) + C + D - E - F);
    
    DistVF(ii,1) = d;
    DistVF(ii,2) = F_A;
    ii = ii + 1;
end

% Load in numerical results
fid = fopen([p_BasePath 'vf_TwoPlates.txt']);
data = textscan(fid,'%f %f','Commentstyle','%');
fclose(fid);
N = size(data{1},1);
DistVFNum = zeros(N,2);
VFError = zeros(N,2);
for ii = 1:N
    DistVFNum(ii,1) = data{1,1}(ii) + dOffset;
    DistVFNum(ii,2) = data{1,2}(ii);
    VFError(ii,1) = data{1,1}(ii) + dOffset;
    VFError(ii,2) = abs(1 - DistVFNum(ii,2) / DistVF(ii,2));
end

% Plot both datasets
figure(1);
plot(DistVF(:,1),DistVF(:,2),'b');
hold on;
plot(DistVFNum(:,1),DistVFNum(:,2),'r');
plot(VFError(:,1),VFError(:,2),'g');
hold off;
title 'parallel plates'
xlabel 'distance'
ylabel 'view factor / error'
legend('analytical','numerical','relative error');
figSetup;

if (f_ExportFigures == 1)
    export_fig([p_BasePath 'TowPlates.png'],'-r100');
end

%% Disc

dOffset = 0.1;
D = dOffset + (0.01:0.01:4.9);

DistVF = zeros(numel(D),2);

a = 0.2;    % Radius disc 1
b = 1.0;    % Radius disc 2

ii = 1;
for d = D

    R1 = a/d;
    R2 = b/d;
    X = 1 + (1 + R2^2)/R1^2;
    Y = X^2 - 4*(R2/R1)^2;

    F_B = 0.5*(X - sqrt(Y));
    
    DistVF(ii,1) = d;
    DistVF(ii,2) = F_B;
    ii = ii + 1;
end

% Load in numerical results
fid = fopen([p_BasePath 'vf_TwoDiscs.txt']);
data = textscan(fid,'%f %f','Commentstyle','%');
fclose(fid);
N = size(data{1},1);
DistVFNum = zeros(N,2);
VFError = zeros(N,2);
for ii = 1:N
    DistVFNum(ii,1) = data{1,1}(ii) + dOffset;
    DistVFNum(ii,2) = data{1,2}(ii);
    VFError(ii,1) = data{1,1}(ii) + dOffset;
    VFError(ii,2) = abs(1 - DistVFNum(ii,2) / DistVF(ii,2));
end

% Plot both datasets
figure(2);
plot(DistVF(:,1),DistVF(:,2),'b');
hold on;
plot(DistVFNum(:,1),DistVFNum(:,2),'r');
plot(VFError(:,1),VFError(:,2),'g');
hold off;
title 'parallel discs'
xlabel 'distance'
ylabel 'view factor / error'
legend('analytical','numerical','relative error');
figSetup;

if (f_ExportFigures == 1)
    export_fig([p_BasePath 'TowDiscs.png'],'-r100');
end

% Results for a = 0.2; b = 1; d = 1
% Numerical: 0.523, analytical: 0.495 => 5% Error
% Results for a = 1.0; b = 0.2; d = 1
% Numerical: 0.029, analytical: 0.02 => 31% Error